using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using codecrafters_redis.src.Bootstrap;
using codecrafters_redis.src.Resp;

namespace codecrafters_redis.src.Cache;

public interface IRDBFileReader
{
  Task ReadAsync(CancellationToken cancellationToken);
}

public sealed class RDBFileReader(IServerOptions serverOptions, ICacheStore cacheStore) : IRDBFileReader
{
  private const byte TypeString = 0x00;

  private const byte OpcodeModuleAux = 0xF7;
  private const byte OpcodeIdle = 0xF8;
  private const byte OpcodeFreq = 0xF9;
  private const byte OpcodeAux = 0xFA;
  private const byte OpcodeResizeDb = 0xFB;
  private const byte OpcodeExpireMilliseconds = 0xFC;
  private const byte OpcodeExpireSeconds = 0xFD;
  private const byte OpcodeSelectDb = 0xFE;
  private const byte OpcodeEof = 0xFF;

  public async Task ReadAsync(CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();

    if (string.IsNullOrWhiteSpace(serverOptions.DataDirectory) || string.IsNullOrWhiteSpace(serverOptions.Dbfilename))
    {
      return;
    }

    string rdbFilePath = Path.Combine(serverOptions.DataDirectory, serverOptions.Dbfilename);
    if (!File.Exists(rdbFilePath))
    {
      return;
    }

    using FileStream fileStream = File.OpenRead(rdbFilePath);
    await LoopOwnerContext.RunOnOwnerLaneAsync(() =>
    {
      ReadHeader(fileStream);
      ReadBody(fileStream, cancellationToken);
      return Task.FromResult(0);
    });
  }

  private static void ReadHeader(Stream stream)
  {
    byte[] header = ReadExactly(stream, 9);

    string magic = Encoding.ASCII.GetString(header, 0, 5);
    if (!string.Equals(magic, "REDIS", StringComparison.Ordinal))
    {
      throw new InvalidOperationException("Invalid RDB file: missing REDIS header.");
    }
  }

  private void ReadBody(Stream stream, CancellationToken cancellationToken)
  {
    long? expiresAtUnixMilliseconds = null;

    while (true)
    {
      cancellationToken.ThrowIfCancellationRequested();

      byte marker = ReadByteRequired(stream);
      switch (marker)
      {
        case OpcodeAux:
          _ = ReadString(stream);
          _ = ReadString(stream);
          break;
        case OpcodeResizeDb:
          _ = ReadLength(stream);
          _ = ReadLength(stream);
          break;
        case OpcodeSelectDb:
          _ = ReadLength(stream);
          expiresAtUnixMilliseconds = null;
          break;
        case OpcodeExpireSeconds:
          expiresAtUnixMilliseconds = BinaryPrimitives.ReadUInt32LittleEndian(ReadExactly(stream, 4)) * 1000L;
          break;
        case OpcodeExpireMilliseconds:
          expiresAtUnixMilliseconds = checked((long)BinaryPrimitives.ReadUInt64LittleEndian(ReadExactly(stream, 8)));
          break;
        case OpcodeIdle:
          _ = ReadLength(stream);
          break;
        case OpcodeFreq:
          _ = ReadByteRequired(stream);
          break;
        case OpcodeModuleAux:
          throw new InvalidOperationException("RDB MODULE AUX opcodes are not supported.");
        case OpcodeEof:
          return;
        default:
          ReadValue(stream, marker, expiresAtUnixMilliseconds);
          expiresAtUnixMilliseconds = null;
          break;
      }
    }
  }

  private void ReadValue(Stream stream, byte valueType, long? expiresAtUnixMilliseconds)
  {
    if (valueType != TypeString)
    {
      throw new InvalidOperationException($"Unsupported RDB value type: 0x{valueType:X2}.");
    }

    string key = ReadString(stream);
    string value = ReadString(stream);

    if (expiresAtUnixMilliseconds.HasValue)
    {
      long ttlMilliseconds = expiresAtUnixMilliseconds.Value - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
      if (ttlMilliseconds <= 0)
      {
        return;
      }

      int boundedTtl = ttlMilliseconds > int.MaxValue ? int.MaxValue : (int)ttlMilliseconds;
      cacheStore.Set(key, CacheValue.String(value), boundedTtl);
      return;
    }

    cacheStore.Set(key, CacheValue.String(value));
  }

  private static string ReadString(Stream stream)
  {
    (long lengthOrEncoding, bool isEncoded) = ReadLength(stream);

    if (!isEncoded)
    {
      if (lengthOrEncoding < 0 || lengthOrEncoding > int.MaxValue)
      {
        throw new InvalidOperationException("RDB string length is out of range.");
      }

      if (lengthOrEncoding == 0)
      {
        return string.Empty;
      }

      byte[] data = ReadExactly(stream, (int)lengthOrEncoding);
      return Encoding.UTF8.GetString(data);
    }

    return lengthOrEncoding switch
    {
      0 => ((sbyte)ReadByteRequired(stream)).ToString(CultureInfo.InvariantCulture),
      1 => BinaryPrimitives.ReadInt16LittleEndian(ReadExactly(stream, 2)).ToString(CultureInfo.InvariantCulture),
      2 => BinaryPrimitives.ReadInt32LittleEndian(ReadExactly(stream, 4)).ToString(CultureInfo.InvariantCulture),
      3 => throw new InvalidOperationException("LZF-compressed strings are not supported."),
      _ => throw new InvalidOperationException($"Unsupported RDB string encoding: {lengthOrEncoding}."),
    };
  }

  private static (long Value, bool IsEncoded) ReadLength(Stream stream)
  {
    byte first = ReadByteRequired(stream);
    byte lengthType = (byte)((first & 0xC0) >> 6);

    switch (lengthType)
    {
      case 0:
        return (first & 0x3F, false);
      case 1:
        {
          byte second = ReadByteRequired(stream);
          return ((((long)first & 0x3F) << 8) | second, false);
        }
      case 2:
        {
          if (first == 0x80)
          {
            return (BinaryPrimitives.ReadUInt32BigEndian(ReadExactly(stream, 4)), false);
          }

          if (first == 0x81)
          {
            ulong value = BinaryPrimitives.ReadUInt64BigEndian(ReadExactly(stream, 8));
            if (value > long.MaxValue)
            {
              throw new InvalidOperationException("64-bit RDB length exceeds supported range.");
            }

            return ((long)value, false);
          }

          throw new InvalidOperationException($"Unsupported RDB length encoding marker: 0x{first:X2}.");
        }
      case 3:
        return (first & 0x3F, true);
      default:
        throw new InvalidOperationException("Invalid RDB length encoding.");
    }
  }

  private static byte ReadByteRequired(Stream stream)
  {
    int value = stream.ReadByte();
    if (value < 0)
    {
      throw new EndOfStreamException("Unexpected end of RDB file.");
    }

    return (byte)value;
  }

  private static byte[] ReadExactly(Stream stream, int count)
  {
    byte[] buffer = new byte[count];
    int offset = 0;

    while (offset < count)
    {
      int read = stream.Read(buffer, offset, count - offset);
      if (read == 0)
      {
        throw new EndOfStreamException("Unexpected end of RDB file.");
      }

      offset += read;
    }

    return buffer;
  }
}
