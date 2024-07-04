using System;
using System.Windows.Input;
using ReactiveUI;
using System.Threading.Tasks;
using UEParser.Views;

namespace UEParser.ViewModels;

public class ParsingControllersViewModel : ReactiveObject
{
    private static readonly Lazy<ParsingControllersViewModel> lazy = new(() => new());
    public static ParsingControllersViewModel Instance => lazy.Value;

    public ICommand? ParseEverythingCommand { get; }
    public ICommand? ParseRiftsCommand { get; }
    public ICommand? ParseCharactersCommand { get; }

    private ParsingControllersViewModel()
    {
        ParseEverythingCommand = ReactiveCommand.Create(ParseEverything);
        ParseRiftsCommand = ReactiveCommand.Create(ParseRifts);
        ParseCharactersCommand = ReactiveCommand.Create(ParseCharacters);
    }

    private void ParseEverything()
    {
        LogsWindowViewModel.Instance.AddLog("Parsing all data..", Logger.LogTags.Info);
        LogsWindowViewModel.Instance.AddLog("Data parsed successfully.", Logger.LogTags.Success);
    }

    private async Task ParseRifts()
    {
        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);

        await APIComposers.Rifts.InitializeRiftsDB();

        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
    }

    private void ParseCharacters()
    {
        LogsWindowViewModel.Instance.AddLog("[Characters] Parsing data..", Logger.LogTags.Info);
        LogsWindowViewModel.Instance.AddLog("[Characters] Data parsed successfully.", Logger.LogTags.Success);
    }
}