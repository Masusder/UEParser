using System;
using System.Windows.Input;
using System.Threading.Tasks;
using ReactiveUI;
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
    public ICommand ParseCharacterClassesCommand { get; }
    public ICommand ParseTomesCommand { get; }
    public ICommand ParseAddonsCommand { get; }
    public ICommand ParseItemsCommand { get; }
    public ICommand ParseOfferingsCommand { get; }
    public ICommand ParseMapsCommand { get; }
    public ICommand ParseDlcsCommand { get; }
    public ICommand ParseJournalsCommand { get; }
    public ICommand ParseSpecialEventsCommand { get; }
    public ICommand ParseCollectionsCommand { get; }
    public ICommand ParseBundlesCommand { get; }

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
        ParseCharacterClassesCommand = ReactiveCommand.Create(ParseCharacterClasses);
        ParseTomesCommand = ReactiveCommand.Create(ParseTomes);
        ParseAddonsCommand = ReactiveCommand.Create(ParseAddons);
        ParseItemsCommand = ReactiveCommand.Create(ParseItems);
        ParseOfferingsCommand = ReactiveCommand.Create(ParseOfferings);
        ParseMapsCommand = ReactiveCommand.Create(ParseMaps);
        ParseDlcsCommand = ReactiveCommand.Create(ParseDlcs);
        ParseJournalsCommand = ReactiveCommand.Create(ParseJournals);
        ParseSpecialEventsCommand = ReactiveCommand.Create(ParseSpecialEvents);
        ParseCollectionsCommand = ReactiveCommand.Create(ParseCollections);
        ParseBundlesCommand = ReactiveCommand.Create(ParseBundles);
    }

    private async void ParseEverything()
    {
        IsParsing = true;
        await ParseRifts();
        await ParseCharacters();
        await ParseCosmetics();
        await ParsePerks();
        await ParseCharacterClasses();
        await ParseTomes();
        await ParseAddons();
        await ParseItems();
        await ParseAddons();
        await ParseOfferings();
        await ParseMaps();
        await ParseDlcs();
        await ParseJournals();
        await ParseSpecialEvents();
        await ParseCollections();
        await ParseBundles();
        IsParsing = false;
    }

    private async Task ParseRifts()
    {
        try
        {
            IsParsing = true;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);

            await Rifts.InitializeRiftsDB();

            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
            IsParsing = false;
        }
        catch (TypeInitializationException ex)
        {
            IsParsing = false;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"Error occured, make sure you meet requirements of this parser! {ex.Message}", Logger.LogTags.Error);
        }
        catch (Exception ex)
        {
            IsParsing = false;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"{ex.Message}", Logger.LogTags.Error);
        }
    }

    private async Task ParseCharacters()
    {
        try
        {
            IsParsing = true;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);

            await Characters.InitializeCharactersDB();

            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
            IsParsing = false;
        }
        catch (Exception ex)
        {
            IsParsing = false;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"{ex.Message}", Logger.LogTags.Error);
        }
    }

    private async Task ParseCosmetics()
    {
        try
        {
            IsParsing = true;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);

            await Cosmetics.InitializeCosmeticsDB();

            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
            IsParsing = false;
        }
        catch (TypeInitializationException ex)
        {
            IsParsing = false;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"Error occured, make sure you meet requirements of this parser! {ex.Message}", Logger.LogTags.Error);
        }
        catch (Exception ex)
        {
            IsParsing = false;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"{ex.Message}", Logger.LogTags.Error);
        }
    }

    private async Task ParsePerks()
    {
        try
        {
            IsParsing = true;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);

            await Perks.InitializePerksDB();

            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
            IsParsing = false;
        }
        catch (Exception ex)
        {
            IsParsing = false;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"{ex.Message}", Logger.LogTags.Error);
        }
    }

    private async Task ParseCharacterClasses()
    {
        try
        {
            IsParsing = true;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);

            await CharacterClasses.InitializeCharacterClassesDB();

            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
            IsParsing = false;
        }
        catch (Exception ex)
        {
            IsParsing = false;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"{ex.Message}", Logger.LogTags.Error);
        }
    }

    private async Task ParseTomes()
    {
        try
        {
            IsParsing = true;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);

            await Tomes.InitializeTomesDB();

            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
            IsParsing = false;
        }
        catch (TypeInitializationException ex)
        {
            IsParsing = false;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"Error occured, make sure you meet requirements of this parser! {ex.Message}", Logger.LogTags.Error);
        }
        catch (Exception ex)
        {
            IsParsing = false;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"{ex.Message}", Logger.LogTags.Error);
        }
    }

    private async Task ParseAddons()
    {
        try
        {
            IsParsing = true;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);

            await Addons.InitializeAddonsDB();

            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
            IsParsing = false;
        }
        catch (Exception ex)
        {
            IsParsing = false;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"{ex.Message}", Logger.LogTags.Error);
        }
    }

    private async Task ParseItems()
    {
        try
        {
            IsParsing = true;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);

            await Items.InitializeItemsDB();

            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
            IsParsing = false;
        }
        catch (Exception ex)
        {
            IsParsing = false;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"{ex.Message}", Logger.LogTags.Error);
        }
    }

    private async Task ParseOfferings()
    {
        try
        {
            IsParsing = true;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);

            await Offerings.InitializeOfferingsDB();

            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
            IsParsing = false;
        }
        catch (Exception ex)
        {
            IsParsing = false;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"{ex.Message}", Logger.LogTags.Error);
        }
    }

    private async Task ParseMaps()
    {
        try
        {
            IsParsing = true;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);

            await Maps.InitializeMapsDB();

            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
            IsParsing = false;
        }
        catch (Exception ex)
        {
            IsParsing = false;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"{ex.Message}", Logger.LogTags.Error);
        }
    }

    private async Task ParseDlcs()
    {
        try
        {
            IsParsing = true;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);

            await DLCs.InitializeDlcsDB();

            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
            IsParsing = false;
        }
        catch (Exception ex)
        {
            IsParsing = false;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"{ex.Message}", Logger.LogTags.Error);
        }
    }

    private async Task ParseJournals()
    {
        try
        {
            IsParsing = true;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);

            await Journals.InitializeJournalsDB();

            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
            IsParsing = false;
        }
        catch (Exception ex)
        {
            IsParsing = false;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"{ex.Message}", Logger.LogTags.Error);
        }
    }

    private async Task ParseSpecialEvents()
    {
        try
        {
            IsParsing = true;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);

            await SpecialEvents.InitializeSpecialEventsDB();

            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
            IsParsing = false;
        }
        catch (Exception ex)
        {
            IsParsing = false;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"{ex.Message}", Logger.LogTags.Error);
        }
    }

    private async Task ParseCollections()
    {
        try
        {
            IsParsing = true;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);

            await Collections.InitializeCollectionsDB();

            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
            IsParsing = false;
        }
        catch (TypeInitializationException ex)
        {
            IsParsing = false;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"Error occured, make sure you meet requirements of this parser! {ex.Message}", Logger.LogTags.Error);
        }
        catch (Exception ex)
        {
            IsParsing = false;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"{ex.Message}", Logger.LogTags.Error);
        }
    }

    private async Task ParseBundles()
    {
        try
        {
            IsParsing = true;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);

            await Bundles.InitializeBundlesDB();

            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
            IsParsing = false;
        }
        catch (TypeInitializationException ex)
        {
            IsParsing = false;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"Error occured, make sure you meet requirements of this parser! {ex.Message}", Logger.LogTags.Error);
        }
        catch (Exception ex)
        {
            IsParsing = false;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"{ex.Message}", Logger.LogTags.Error);
        }
    }
}