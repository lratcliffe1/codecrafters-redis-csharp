using System.Threading.Channels;
using codecrafters_redis.src.Helpers;

namespace codecrafters_redis.src.Resp;

public interface ICommandEventLoop
{
  Task<string> ExecuteAsync(RespValue value, long clientId, int port, CancellationToken cancellationToken);
  ValueTask NotifyClientDisconnectedAsync(long clientId, int port);
  ValueTask DisposeAsync();
}

internal sealed class CommandEventLoop : ICommandEventLoop, IAsyncDisposable
{
  private enum EnvelopeType
  {
    Execute,
    ClientDisconnected,
  }

  private readonly ConcurrentExclusiveSchedulerPair _loopSchedulerPair = new();
  private readonly TaskFactory _loopTaskFactory;
  private readonly Channel<CommandEnvelope> _queue = Channel.CreateUnbounded<CommandEnvelope>(
    new UnboundedChannelOptions
    {
      SingleReader = true,
      SingleWriter = false,
    });
  private readonly Task _processorTask;
  private readonly IRespExecutor _respExecutor;

  public CommandEventLoop(IRespExecutor respExecutor)
  {
    _respExecutor = respExecutor;
    _loopTaskFactory = new TaskFactory(
      CancellationToken.None,
      TaskCreationOptions.DenyChildAttach,
      TaskContinuationOptions.None,
      _loopSchedulerPair.ExclusiveScheduler);

    _processorTask = Task.Run(ProcessLoopAsync);
  }

  public async Task<string> ExecuteAsync(RespValue value, long clientId, int port, CancellationToken cancellationToken)
  {
    TaskCompletionSource<string> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    CommandEnvelope envelope = new(EnvelopeType.Execute, value, clientId, port, completion, cancellationToken);

    await _queue.Writer.WriteAsync(envelope, cancellationToken);
    return await completion.Task.WaitAsync(cancellationToken);
  }

  public ValueTask NotifyClientDisconnectedAsync(long clientId, int port)
  {
    CommandEnvelope envelope = new(EnvelopeType.ClientDisconnected, null, clientId, port, null, CancellationToken.None);
    return _queue.Writer.WriteAsync(envelope);
  }

  private async Task ProcessLoopAsync()
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

  private void ProcessCommandEnvelope(CommandEnvelope envelope)
  {
    if (envelope.Value == null || envelope.Completion == null)
    {
      return;
    }

    try
    {
      Task<string> executionTask = _loopTaskFactory
        .StartNew(
          () => LoopOwnerContext.RunOnOwnerLaneAsync(() => _respExecutor.ExecuteAsync(envelope.Value, envelope.ClientId, envelope.Port, envelope.CancellationToken)),
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
      envelope.Completion.TrySetResult(CommandHelper.BuildError($"internal server error: {exception.Message}"));
    }
  }

  private Task<int> ProcessClientDisconnectedAsync(long clientId)
  {
    return _loopTaskFactory
      .StartNew(
        () => LoopOwnerContext.RunOnOwnerLaneAsync(() =>
        {
          _respExecutor.OnClientDisconnected(clientId);
          return Task.FromResult(0);
        }),
        CancellationToken.None)
      .Unwrap();
  }

  private static void CompleteEnvelope(TaskCompletionSource<string> completion, Task<string> executionTask)
  {
    if (executionTask.IsCanceled)
    {
      completion.TrySetResult(CommandHelper.FormatNull(RespType.Array));
      return;
    }

    if (executionTask.IsFaulted)
    {
      string message = executionTask.Exception?.GetBaseException().Message ?? "unknown";
      completion.TrySetResult(CommandHelper.BuildError($"internal server error: {message}"));
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

  public async ValueTask DisposeAsync()
  {
    _queue.Writer.TryComplete();
    _loopSchedulerPair.Complete();

    try
    {
      await _processorTask.ConfigureAwait(false);
    }
    catch (Exception) { }
  }
}
