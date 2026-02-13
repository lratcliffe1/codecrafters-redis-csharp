using System.Threading.Channels;
using codecrafters_redis.src.Helpers;

namespace codecrafters_redis.src.Resp;

internal static class CommandEventLoop
{
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

  public static async Task<string> ExecuteAsync(RespValue value, long clientId, CancellationToken cancellationToken)
  {
    TaskCompletionSource<string> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    CommandEnvelope envelope = new(value, clientId, completion, cancellationToken);

    await _queue.Writer.WriteAsync(envelope, cancellationToken);
    return await completion.Task.WaitAsync(cancellationToken);
  }

  private static async Task ProcessLoopAsync()
  {
    await foreach (CommandEnvelope envelope in _queue.Reader.ReadAllAsync())
    {
      try
      {
        Task<string> executionTask = _loopTaskFactory
          .StartNew(
            () => LoopOwnerContext.RunOnOwnerLaneAsync(() => RespExecutor.ExecuteAsync(envelope.Value, envelope.ClientId, envelope.CancellationToken)),
            envelope.CancellationToken)
          .Unwrap();

        if (executionTask.IsCompleted)
        {
          CompleteEnvelope(envelope, executionTask);
          continue;
        }

        _ = executionTask.ContinueWith(
          _ => CompleteEnvelope(envelope, executionTask),
          CancellationToken.None,
          TaskContinuationOptions.ExecuteSynchronously,
          TaskScheduler.Default);
      }
      catch (Exception exception)
      {
        envelope.Completion.TrySetResult(CommandHepler.BuildError($"internal server error: {exception.Message}"));
      }
    }
  }

  private static void CompleteEnvelope(CommandEnvelope envelope, Task<string> executionTask)
  {
    if (executionTask.IsCanceled)
    {
      envelope.Completion.TrySetResult(CommandHepler.FormatNull(RespType.Array));
      return;
    }

    if (executionTask.IsFaulted)
    {
      string message = executionTask.Exception?.GetBaseException().Message ?? "unknown";
      envelope.Completion.TrySetResult(CommandHepler.BuildError($"internal server error: {message}"));
      return;
    }

    envelope.Completion.TrySetResult(executionTask.Result);
  }

  private sealed record CommandEnvelope(
    RespValue Value,
    long ClientId,
    TaskCompletionSource<string> Completion,
    CancellationToken CancellationToken);
}
