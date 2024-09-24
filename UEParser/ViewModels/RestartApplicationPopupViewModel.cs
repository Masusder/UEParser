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
}