namespace WinWidget.Services;

public sealed class SingleInstanceService : IDisposable
{
    private readonly Mutex _mutex;
    private readonly EventWaitHandle? _activationEvent;
    private RegisteredWaitHandle? _activationWait;
    public bool IsPrimaryInstance { get; }
    public event EventHandler? ActivationRequested;

    public SingleInstanceService(string applicationId = "WinWidget.Windows11")
    {
        _mutex = new Mutex(true, $"Local\\{applicationId}", out var createdNew);
        IsPrimaryInstance = createdNew;

        var activationEventName = $"Local\\{applicationId}.Activate";
        if (IsPrimaryInstance)
        {
            _activationEvent = new EventWaitHandle(false, EventResetMode.AutoReset, activationEventName);
        }
        else
        {
            for (var attempt = 0; attempt < 20; attempt++)
            {
                try
                {
                    using var activationEvent = EventWaitHandle.OpenExisting(activationEventName);
                    activationEvent.Set();
                    break;
                }
                catch (WaitHandleCannotBeOpenedException) when (attempt < 19)
                {
                    // The primary owns the mutex but may still be creating the activation event.
                    Thread.Sleep(50);
                }
                catch (WaitHandleCannotBeOpenedException)
                {
                    // Do not show an error if the primary exits during the short startup race.
                }
            }
        }
    }

    public void StartListening()
    {
        if (!IsPrimaryInstance || _activationEvent is null || _activationWait is not null) return;
        _activationWait = ThreadPool.RegisterWaitForSingleObject(
            _activationEvent,
            (_, timedOut) =>
            {
                if (!timedOut) ActivationRequested?.Invoke(this, EventArgs.Empty);
            },
            null,
            Timeout.Infinite,
            false);
    }

    public void Dispose()
    {
        _activationWait?.Unregister(null);
        _activationEvent?.Dispose();
        if (IsPrimaryInstance) _mutex.ReleaseMutex();
        _mutex.Dispose();
    }
}
