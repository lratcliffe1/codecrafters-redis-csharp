using System.Threading.Channels;
using codecrafters_redis.src.Helpers;

namespace codecrafters_redis.src.Resp;

internal static class CommandEventLoop
{
  private enum EnvelopeType
  {
    Execute,
    ClientDisconnected,
  }

  private static readonly ConcurrentExclusiveSchedulerPair _loopSchedulerPair = new();
  private static readonly TaskFactory _loopTaskFactory = new(
    CancellationToken.None,
    TaskCreationOptions.DenyChildAttach,
    TaskContinuationOptions.None,
    _loopSchedulerPair.ExclusiveScheduler);

  private static readonly Channel<CommandEnvelope> _queue = Channel.CreateUnbounded<CommandEnvelope>(
    new UnboundedChannelOptions
    {
      SingleReader = true,
      SingleWriter = false,
    });

  private static readonly Task _processorTask = Task.Run(ProcessLoopAsync);

  public static async Task<string> ExecuteAsync(RespValue value, long clientId, int port, CancellationToken cancellationToken)
  {
    TaskCompletionSource<string> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    CommandEnvelope envelope = new(EnvelopeType.Execute, value, clientId, port, completion, cancellationToken);

    await _queue.Writer.WriteAsync(envelope, cancellationToken);
    return await completion.Task.WaitAsync(cancellationToken);
  }

  public static ValueTask NotifyClientDisconnectedAsync(long clientId, int port)
  {
    CommandEnvelope envelope = new(EnvelopeType.ClientDisconnected, null, clientId, port, null, CancellationToken.None);
    return _queue.Writer.WriteAsync(envelope);
  }

  private static async Task ProcessLoopAsync()
  {
    await foreach (CommandEnvelope envelope in _queue.Reader.ReadAllAsync())
    {
      switch (envelope.Type)
      {
        case EnvelopeType.Execute:
          ProcessCommandEnvelope(envelope);
          break;
        case EnvelopeType.ClientDisconnected:
          await ProcessClientDisconnectedAsync(envelope.ClientId);
          break;
      }
    }
  }

  private static void ProcessCommandEnvelope(CommandEnvelope envelope)
  {
    if (envelope.Value == null || envelope.Completion == null)
    {
      return;
    }

    try
    {
      Task<string> executionTask = _loopTaskFactory
        .StartNew(
          () => LoopOwnerContext.RunOnOwnerLaneAsync(() => RespExecutor.ExecuteAsync(envelope.Value, envelope.ClientId, envelope.Port, envelope.CancellationToken)),
          envelope.CancellationToken)
        .Unwrap();

      if (executionTask.IsCompleted)
      {
        CompleteEnvelope(envelope.Completion, executionTask);
        return;
      }

      _ = executionTask.ContinueWith(
        _ => CompleteEnvelope(envelope.Completion, executionTask),
        CancellationToken.None,
        TaskContinuationOptions.ExecuteSynchronously,
        TaskScheduler.Default);
    }
    catch (Exception exception)
    {
      envelope.Completion.TrySetResult(CommandHepler.BuildError($"internal server error: {exception.Message}"));
    }
  }

  private static Task<int> ProcessClientDisconnectedAsync(long clientId)
  {
    return _loopTaskFactory
      .StartNew(
        () => LoopOwnerContext.RunOnOwnerLaneAsync(() =>
        {
          RespExecutor.OnClientDisconnected(clientId);
          return Task.FromResult(0);
        }),
        CancellationToken.None)
      .Unwrap();
  }

  private static void CompleteEnvelope(TaskCompletionSource<string> completion, Task<string> executionTask)
  {
    if (executionTask.IsCanceled)
    {
      completion.TrySetResult(CommandHepler.FormatNull(RespType.Array));
      return;
    }

    if (executionTask.IsFaulted)
    {
      string message = executionTask.Exception?.GetBaseException().Message ?? "unknown";
      completion.TrySetResult(CommandHepler.BuildError($"internal server error: {message}"));
      return;
    }

    completion.TrySetResult(executionTask.Result);
  }

  private sealed record CommandEnvelope(
    EnvelopeType Type,
    RespValue? Value,
    long ClientId,
    int Port,
    TaskCompletionSource<string>? Completion,
    CancellationToken CancellationToken);
}
