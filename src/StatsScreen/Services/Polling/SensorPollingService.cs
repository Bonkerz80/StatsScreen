using System.Diagnostics;
using StatsScreen.Models;
using StatsScreen.Services.Hardware;
using StatsScreen.Services.Logging;

namespace StatsScreen.Services.Polling;

public sealed class SensorPollingService : IDisposable
{
    private readonly object _sync = new();
    private readonly HardwareMonitorService _hardwareMonitor;
    private readonly IAppLogger _logger;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _pollingTask;
    private int _intervalMilliseconds;
    private bool _disposed;

    public SensorPollingService(
        HardwareMonitorService hardwareMonitor,
        IAppLogger logger,
        int intervalMilliseconds)
    {
        _hardwareMonitor = hardwareMonitor;
        _logger = logger;
        _intervalMilliseconds = Math.Clamp(intervalMilliseconds, 250, 10_000);
    }

    public event EventHandler<DashboardSnapshot>? SnapshotAvailable;

    public int IntervalMilliseconds => Volatile.Read(ref _intervalMilliseconds);

    public void Start()
    {
        lock (_sync)
        {
            if (_disposed || _pollingTask is { IsCompleted: false })
                return;

            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = _cancellationTokenSource.Token;
            _pollingTask = Task.Run(() => PollAsync(token), CancellationToken.None);
        }
    }

    public void Restart(int intervalMilliseconds)
    {
        Stop();
        Volatile.Write(ref _intervalMilliseconds, Math.Clamp(intervalMilliseconds, 250, 10_000));
        Start();
    }

    public void Stop()
    {
        Task? pollingTask;
        lock (_sync)
        {
            _cancellationTokenSource?.Cancel();
            pollingTask = _pollingTask;
            _pollingTask = null;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        if (pollingTask is not null && Task.CurrentId != pollingTask.Id)
        {
            try
            {
                pollingTask.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping the polling loop.
            }
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
                return;

            _disposed = true;
        }

        Stop();
    }

    private async Task PollAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            DashboardSnapshot snapshot = _hardwareMonitor.ReadSnapshot();
            Publish(snapshot);
            stopwatch.Stop();

            TimeSpan delay = TimeSpan.FromMilliseconds(IntervalMilliseconds) - stopwatch.Elapsed;
            if (delay > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
            else
            {
                await Task.Yield();
            }
        }
    }

    private void Publish(DashboardSnapshot snapshot)
    {
        try
        {
            SnapshotAvailable?.Invoke(this, snapshot);
        }
        catch (Exception exception)
        {
            _logger.Error("A dashboard snapshot subscriber failed.", exception);
        }
    }
}
