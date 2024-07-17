using ReactiveUI;
using System;
using System.Reactive;
using System.Threading.Tasks;
using UEParser;

namespace UEParser.ViewModels;

public class InitializationConfirmPopupViewModel : ReactiveObject
{
    public event Action<bool>? CloseAction; // Event to notify the view to close the popup

    public ReactiveCommand<Unit, Unit> YesCommand { get; }
    public ReactiveCommand<Unit, Unit> NoCommand { get; }

    public InitializationConfirmPopupViewModel()
    {
        YesCommand = ReactiveCommand.Create(OnYesClicked);
        NoCommand = ReactiveCommand.Create(OnNoClicked);
    }

    private void OnYesClicked()
    {
        CloseAction?.Invoke(true);
    }

    private void OnNoClicked()
    {
        // Notify the view to close the popup
        CloseAction?.Invoke(false);
    }
}
