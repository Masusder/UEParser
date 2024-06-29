using Avalonia.Threading;
using ReactiveUI;
using System;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using System.Windows.Input;
using UEParser.Parser;

namespace UEParser.ViewModels;

public class AssetsExtractorViewModel
{
    public ICommand ExtractMissingAssetsCommand { get; }

    public AssetsExtractorViewModel() 
    {
        ExtractMissingAssetsCommand = ReactiveCommand.Create(ExtractMissingAssets);
    }

    private async void ExtractMissingAssets()
    {
        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);
        var newAssets = FilesRegister.NewAssets;


            foreach (var file in newAssets)
            {
                await Task.Delay(100); // Update every 100ms
                // Marshal the UI update to the UI thread
                Dispatcher.UIThread.Post(() =>
                {
                    LogsWindowViewModel.Instance.AddLog($"New asset: {file.Key}", Logger.LogTags.Info);
                });
            }


        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
    }
}
