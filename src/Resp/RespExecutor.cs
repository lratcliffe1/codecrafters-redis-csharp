using codecrafters_redis.src.Commands;
using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace codecrafters_redis.src.Resp;

public interface IRespExecutor
{
  Task<string> ExecuteAsync(RespValue value, long clientId, int port, CancellationToken cancellationToken = default);
  void OnClientDisconnected(long clientId);
}

public sealed class RespExecutor(
  IClientMultiStore clientMultiStore,
  IServiceProvider serviceProvider,
  [FromKeyedServices("DISCARD")] IRedisCommand discardCommand,
  [FromKeyedServices("MULTI")] IRedisCommand multiCommand) : IRespExecutor
{
  private readonly IClientMultiStore _clientMultiStore = clientMultiStore;
  private readonly IServiceProvider _serviceProvider = serviceProvider;
  private readonly IRedisCommand _discardCommand = discardCommand;
  private readonly IRedisCommand _multiCommand = multiCommand;

  public Task<string> ExecuteAsync(RespValue value, long clientId, int port, CancellationToken cancellationToken = default)
  {
    if (!TryReadCommand(value, out string command))
    {
      return CommandHelper.BuildErrorAsync("expected array command");
    }

    if (_clientMultiStore.ContainsKey(clientId))
    {
      return ExecuteInMultiAsync(value, command, clientId, port, cancellationToken);
    }

    return ExecuteCommandAsync(value, command, clientId, port, cancellationToken);
  }

  public void OnClientDisconnected(long clientId)
  {
    _clientMultiStore.Remove(clientId);
  }

  private static bool TryReadCommand(RespValue value, out string command)
  {
    command = string.Empty;
    var args = value.ArrayValue;
    if (value.Type != RespType.Array || args == null || args.Count == 0)
    {
      return false;
    }

    command = args[0].ToString().ToUpperInvariant();
    return true;
  }

  private Task<string> ExecuteInMultiAsync(
    RespValue originalValue,
    string command,
    long clientId,
    int port,
    CancellationToken cancellationToken)
  {
    CommandExecutionContext context = new(clientId, port, originalValue, cancellationToken);

    return command switch
    {
      "EXEC" => ExecCommandAsync(clientId, port, cancellationToken),
      "MULTI" => CommandHelper.BuildErrorAsync("MULTI calls can not be nested"),
      "DISCARD" => _discardCommand.ExecuteAsync(originalValue.ArrayValue ?? [], context),
      _ => _multiCommand.ExecuteAsync(originalValue.ArrayValue ?? [], context),
    };
  }

  private Task<string> ExecuteCommandAsync(
    RespValue originalValue,
    string command,
    long clientId,
    int port,
    CancellationToken cancellationToken)
  {
    if (string.Equals(command, "EXEC", StringComparison.OrdinalIgnoreCase))
    {
      return ExecCommandAsync(clientId, port, cancellationToken);
    }

    if (string.Equals(command, "DISCARD", StringComparison.OrdinalIgnoreCase))
    {
      return CommandHelper.BuildErrorAsync("DISCARD without MULTI");
    }

    var redisCommand = _serviceProvider.GetKeyedService<IRedisCommand>(command.ToUpper());

    if (redisCommand != null)
    {
      CommandExecutionContext context = new(clientId, port, originalValue, cancellationToken);
      return redisCommand.ExecuteAsync(originalValue.ArrayValue ?? [], context);
    }

    return CommandHelper.BuildErrorAsync($"unknown command: {command}");
  }

  private async Task<string> ExecCommandAsync(long clientId, int port, CancellationToken cancellationToken)
  {
    if (!_clientMultiStore.TryGetValue(clientId, out var commands) || commands == null)
    {
      return CommandHelper.BuildError("EXEC without MULTI");
    }

    _clientMultiStore.Remove(clientId);

    List<string> results = [];
    foreach (RespValue command in commands)
    {
      string result = await ExecuteAsync(command, clientId, port, cancellationToken);
      results.Add(result);
    }

    return CommandHelper.FormatArrayOfResp(results);
  }
}
