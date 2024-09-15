using System;
using System.Threading;
using UEParser.ViewModels;

namespace UEParser.Services;

public class CancellationTokenService
{
    private static readonly Lazy<CancellationTokenService> _instance =
        new(() => new CancellationTokenService());

    public static CancellationTokenService Instance => _instance.Value;

    private CancellationTokenSource _cts = new();

    private CancellationTokenService() { }

    public CancellationToken Token => _cts.Token;

    public void Cancel()
    {
        // Only allow to cancel if task is running with possibility of cancellation
        if (LogsWindowViewModel.Instance.LogState == LogsWindowViewModel.ELogState.RunningWithCancellation)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Cancellation); // Notify the user that the task cancellation is in progress
            _cts.Cancel();
            _cts.Dispose();
            _cts = new CancellationTokenSource();
        }
    }
}
