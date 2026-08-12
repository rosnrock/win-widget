namespace WinWidget.Services;

public sealed class SingleInstanceService : IDisposable
{
    private readonly Mutex _mutex;
    public bool IsPrimaryInstance { get; }

    public SingleInstanceService(string applicationId = "WinWidget.Windows11")
    {
        _mutex = new Mutex(true, $"Local\\{applicationId}", out var createdNew);
        IsPrimaryInstance = createdNew;
    }

    public void Dispose()
    {
        if (IsPrimaryInstance) _mutex.ReleaseMutex();
        _mutex.Dispose();
    }
}
