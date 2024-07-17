using System;
using System.Windows.Input;
using ReactiveUI;
using System.Threading.Tasks;
using UEParser.APIComposers;

namespace UEParser.ViewModels;

public class StringResources
{
    public required string Title { get; set; }
    public required string Description { get; set; }
    public string? Requirements { get; set; }
}

public class ParsingControllersViewModel : ReactiveObject
{
    private static readonly Lazy<ParsingControllersViewModel> lazy = new(() => new());
    public static ParsingControllersViewModel Instance => lazy.Value;

    public ICommand ParseEverythingCommand { get; }
    public ICommand ParseRiftsCommand { get; }
    public ICommand ParseCharactersCommand { get; }
    public ICommand ParseCosmeticsCommand { get; }
    public ICommand ParsePerksCommand { get; }
    public ICommand ParseTomesCommand { get; }
    public ICommand ParseAddonsCommand { get; }
    public ICommand ParseItemsCommand { get; }
    public ICommand ParseOfferingsCommand { get; }
    public ICommand ParseMapsCommand { get; }
    public ICommand ParseDlcsCommand { get; }

    private bool _isParsing;
    public bool IsParsing
    {
        get => _isParsing;
        set => this.RaiseAndSetIfChanged(ref _isParsing, value);
    }

    private ParsingControllersViewModel()
    {
        ParseEverythingCommand = ReactiveCommand.Create(ParseEverything);
        ParseRiftsCommand = ReactiveCommand.Create(ParseRifts);
        ParseCharactersCommand = ReactiveCommand.Create(ParseCharacters);
        ParseCosmeticsCommand = ReactiveCommand.Create(ParseCosmetics);
        ParsePerksCommand = ReactiveCommand.Create(ParsePerks);
        ParseTomesCommand = ReactiveCommand.Create(ParseTomes);
        ParseAddonsCommand = ReactiveCommand.Create(ParseAddons);
        ParseItemsCommand = ReactiveCommand.Create(ParseItems);
        ParseOfferingsCommand = ReactiveCommand.Create(ParseOfferings);
        ParseMapsCommand = ReactiveCommand.Create(ParseMaps);
        ParseDlcsCommand = ReactiveCommand.Create(ParseDlcs);
    }

    private async void ParseEverything()
    {
        IsParsing = true;
        await ParseRifts();
        await ParseCharacters();
        await ParseCosmetics();
        await ParsePerks();
        await ParseTomes();
        await ParseAddons();
        await ParseItems();
        await ParseAddons();
        await ParseOfferings();
        await ParseMaps();
        await ParseDlcs();
        IsParsing = false;
    }

    private async Task ParseRifts()
    {
        IsParsing = true;
        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);

        await Rifts.InitializeRiftsDB();

        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
        IsParsing = false;
    }

    private async Task ParseCharacters()
    {
        IsParsing = true;
        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);

        await Characters.InitializeCharactersDB();

        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
        IsParsing = false;
    }

    private async Task ParseCosmetics()
    {
        IsParsing = true;
        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);

        await Cosmetics.InitializeCosmeticsDB();

        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
        IsParsing = false;
    }

    private async Task ParsePerks()
    {
        IsParsing = true;
        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);

        await Perks.InitializePerksDB();

        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
        IsParsing = false;
    }

    private async Task ParseTomes()
    {
        IsParsing = true;
        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);

        await Tomes.InitializeTomesDB();

        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
        IsParsing = false;
    }

    private async Task ParseAddons()
    {
        IsParsing = true;
        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);

        await Addons.InitializeAddonsDB();

        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
        IsParsing = false;
    }

    private async Task ParseItems()
    {
        IsParsing = true;
        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);

        await Items.InitializeItemsDB();

        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
        IsParsing = false;
    }

    private async Task ParseOfferings()
    {
        IsParsing = true;
        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);

        await Offerings.InitializeOfferingsDB();

        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
        IsParsing = false;
    }

    private async Task ParseMaps()
    {
        IsParsing = true;
        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);

        await Maps.InitializeMapsDB();

        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
        IsParsing = false;
    }

    private async Task ParseDlcs()
    {
        IsParsing = true;
        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);

        await DLCs.InitializeDlcsDB();

        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
        IsParsing = false;
    }
}