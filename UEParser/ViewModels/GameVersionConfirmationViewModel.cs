using System;
using System.Reactive;
using ReactiveUI;

namespace UEParser.ViewModels;

public class GameVersionConfirmationViewModel : ReactiveObject
{
    public event Action<bool>? CloseAction; // Event to notify the view to close the popup

    private string? _detectedVersion;
    public string? DetectedVersion
    {
        get => _detectedVersion;
        set => this.RaiseAndSetIfChanged(ref _detectedVersion, value);
    }

    public ReactiveCommand<Unit, Unit> YesCommand { get; }
    public ReactiveCommand<Unit, Unit> NoCommand { get; }

    public GameVersionConfirmationViewModel(string detectedVersion)
    {
        DetectedVersion = detectedVersion;

        YesCommand = ReactiveCommand.Create(OnYesClicked);
        NoCommand = ReactiveCommand.Create(OnNoClicked);
    }

    private void OnYesClicked()
    {
        CloseAction?.Invoke(true);
    }

    private void OnNoClicked()
    {
        CloseAction?.Invoke(false);
    }
}