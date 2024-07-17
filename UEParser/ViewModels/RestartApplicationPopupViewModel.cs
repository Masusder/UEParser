using ReactiveUI;

namespace UEParser.ViewModels;

public class RestartApplicationPopupViewModel : ReactiveObject
{
    private bool _userConfirmedRestart;

    public bool UserConfirmedRestart
    {
        get => _userConfirmedRestart;
        set => this.RaiseAndSetIfChanged(ref _userConfirmedRestart, value);
    }

    //public ReactiveCommand<Unit, Unit> ConfirmCommand { get; }
    //public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    //public RestartApplicationPopupViewModel()
    //{
    //    ConfirmCommand = ReactiveCommand.Create(() =>
    //    {
    //        UserConfirmedRestart = true;
    //    });

    //    CancelCommand = ReactiveCommand.Create(() =>
    //    {
    //        UserConfirmedRestart = false;
    //    });
    //}
}
