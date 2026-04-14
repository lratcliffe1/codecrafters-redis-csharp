using codecrafters_redis.src.Commands;
using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Commands.Multi;
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
  ITransactionGuard transactionGuard,
  ITransactionStateCleaner transactionStateCleaner,
  IPubSubStore pubSubStore,
  IAclUserStore aclUserStore,
  IClientAuthStore clientAuthStore,
  IServiceProvider serviceProvider,
  [FromKeyedServices("DISCARD")] IRedisCommand discardCommand,
  [FromKeyedServices("MULTI")] IRedisCommand multiCommand) : IRespExecutor
{
  public Task<string> ExecuteAsync(RespValue value, long clientId, int port, CancellationToken cancellationToken = default)
  {
    if (!TryReadCommand(value, out string command))
    {
      return CommandHelper.BuildErrorAsync("expected array command");
    }

    if (RequiresAuthentication(command, clientId))
    {
      return CommandHelper.BuildNamedErrorAsync("NOAUTH", "Authentication required.");
    }

    if (clientMultiStore.ContainsKey(clientId))
    {
      return ExecuteInMultiAsync(value, command, clientId, port, cancellationToken);
    }

    if (pubSubStore.ContainsKey(clientId))
    {
      return ExecuteInPubSubAsync(value, command, clientId, port, cancellationToken);
    }

    return ExecuteCommandAsync(value, command, clientId, port, cancellationToken);
  }

  public void OnClientDisconnected(long clientId)
  {
    transactionStateCleaner.Clear(clientId);
    pubSubStore.Remove(clientId);
    clientAuthStore.Remove(clientId);
  }

  private bool RequiresAuthentication(string command, long clientId)
  {
    if (string.Equals(command, "AUTH", StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }

    if (!aclUserStore.TryGetUser("default", out AclUser defaultUser) || defaultUser.HasNoPassword)
    {
      clientAuthStore.Authenticate(clientId, "default");
      return false;
    }

    return !clientAuthStore.IsAuthenticated(clientId);
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
    CommandExecutionContext context = new(clientId, port, originalValue, CommandMode.Multi, cancellationToken);

    return command switch
    {
      "EXEC" => ExecCommandAsync(clientId, port, cancellationToken),
      "MULTI" => CommandHelper.BuildErrorAsync("MULTI calls can not be nested"),
      "DISCARD" => discardCommand.ExecuteAsync(originalValue.ArrayValue ?? [], context),
      "WATCH" => CommandHelper.BuildErrorAsync("WATCH inside MULTI is not allowed"),
      _ => multiCommand.ExecuteAsync(originalValue.ArrayValue ?? [], context),
    };
  }

  private Task<string> ExecuteInPubSubAsync(
    RespValue originalValue,
    string command,
    long clientId,
    int port,
    CancellationToken cancellationToken)
  {
    CommandExecutionContext context = new(clientId, port, originalValue, CommandMode.PubSub, cancellationToken);

    var redisCommand = serviceProvider.GetKeyedService<IRedisCommand>(command.ToUpper());

    if (redisCommand != null)
    {
      return command switch
      {
        "SUBSCRIBE"
        or "PUBLISH"
        or "PING"
        or "UNSUBSCRIBE" => redisCommand.ExecuteAsync(originalValue.ArrayValue ?? [], context),
        _ => CommandHelper.BuildErrorAsync($"Can't execute '{command}': only (P|S)SUBSCRIBE / (P|S)UNSUBSCRIBE / PING / QUIT / RESET are allowed in this context"),
      };
    }

    return CommandHelper.BuildErrorAsync($"unknown command: {command}");
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

    var redisCommand = serviceProvider.GetKeyedService<IRedisCommand>(command.ToUpper());

    if (redisCommand != null)
    {
      CommandExecutionContext context = new(clientId, port, originalValue, CommandMode.Command, cancellationToken);
      return redisCommand.ExecuteAsync(originalValue.ArrayValue ?? [], context);
    }

    return CommandHelper.BuildErrorAsync($"unknown command: {command}");
  }

  private async Task<string> ExecCommandAsync(long clientId, int port, CancellationToken cancellationToken)
  {
    if (!clientMultiStore.TryGetValue(clientId, out var commands) || commands == null)
    {
      return CommandHelper.BuildError("EXEC without MULTI");
    }

    bool watchedKeyWasModified = transactionGuard.WatchedKeyWasModified(clientId);

    transactionStateCleaner.Clear(clientId);

    if (watchedKeyWasModified)
    {
      return CommandHelper.FormatNull(RespType.Array);
    }

    List<string> results = [];
    foreach (RespValue command in commands)
    {
      string result = await ExecuteAsync(command, clientId, port, cancellationToken);
      results.Add(result);
    }

    return CommandHelper.FormatArrayOfResp(results);
  }
}
