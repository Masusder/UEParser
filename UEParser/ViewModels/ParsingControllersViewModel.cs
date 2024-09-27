using System;
using System.Threading;
using System.Windows.Input;
using System.Threading.Tasks;
using ReactiveUI;
using UEParser.APIComposers;
using UEParser.Services;

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
        ParseEverythingCommand = ReactiveCommand.CreateFromTask(() => ParseEverything(CancellationTokenService.Instance.Token));
        ParseRiftsCommand = ReactiveCommand.CreateFromTask(() => ParseRifts(CancellationTokenService.Instance.Token));
        ParseCharactersCommand = ReactiveCommand.CreateFromTask(() => ParseCharacters(CancellationTokenService.Instance.Token));
        ParseCosmeticsCommand = ReactiveCommand.CreateFromTask(() => ParseCosmetics(CancellationTokenService.Instance.Token));
        ParsePerksCommand = ReactiveCommand.CreateFromTask(() => ParsePerks(CancellationTokenService.Instance.Token));
        ParseCharacterClassesCommand = ReactiveCommand.CreateFromTask(() => ParseCharacterClasses(CancellationTokenService.Instance.Token));
        ParseTomesCommand = ReactiveCommand.CreateFromTask(() => ParseTomes(CancellationTokenService.Instance.Token));
        ParseAddonsCommand = ReactiveCommand.CreateFromTask(() => ParseAddons(CancellationTokenService.Instance.Token));
        ParseItemsCommand = ReactiveCommand.CreateFromTask(() => ParseItems(CancellationTokenService.Instance.Token));
        ParseOfferingsCommand = ReactiveCommand.CreateFromTask(() => ParseOfferings(CancellationTokenService.Instance.Token));
        ParseMapsCommand = ReactiveCommand.CreateFromTask(() => ParseMaps(CancellationTokenService.Instance.Token));
        ParseDlcsCommand = ReactiveCommand.CreateFromTask(() => ParseDlcs(CancellationTokenService.Instance.Token));
        ParseJournalsCommand = ReactiveCommand.CreateFromTask(() => ParseJournals(CancellationTokenService.Instance.Token));
        ParseSpecialEventsCommand = ReactiveCommand.CreateFromTask(() => ParseSpecialEvents(CancellationTokenService.Instance.Token));
        ParseCollectionsCommand = ReactiveCommand.CreateFromTask(() => ParseCollections(CancellationTokenService.Instance.Token));
        ParseBundlesCommand = ReactiveCommand.CreateFromTask(() => ParseBundles(CancellationTokenService.Instance.Token));
    }

    private async Task ParseEverything(CancellationToken token)
    {
        try
        {
            IsParsing = true;
            await Helpers.ExecuteWithCancellation(() => ParseRifts(token), token);
            await Helpers.ExecuteWithCancellation(() => ParseCharacters(token), token);
            await Helpers.ExecuteWithCancellation(() => ParseCosmetics(token), token);
            await Helpers.ExecuteWithCancellation(() => ParsePerks(token), token);
            await Helpers.ExecuteWithCancellation(() => ParseCharacterClasses(token), token);
            await Helpers.ExecuteWithCancellation(() => ParseTomes(token), token);
            await Helpers.ExecuteWithCancellation(() => ParseAddons(token), token);
            await Helpers.ExecuteWithCancellation(() => ParseItems(token), token);
            await Helpers.ExecuteWithCancellation(() => ParseOfferings(token), token);
            await Helpers.ExecuteWithCancellation(() => ParseMaps(token), token);
            await Helpers.ExecuteWithCancellation(() => ParseDlcs(token), token);
            await Helpers.ExecuteWithCancellation(() => ParseJournals(token), token);
            await Helpers.ExecuteWithCancellation(() => ParseSpecialEvents(token), token);
            await Helpers.ExecuteWithCancellation(() => ParseCollections(token), token);
            await Helpers.ExecuteWithCancellation(() => ParseBundles(token), token);
        }
        catch
        {
            // do nothing, it's alredy logged in the parsers
        }
        finally
        {
            IsParsing = false;
        }
    }

    private async Task ParseRifts(CancellationToken token)
    {
        try
        {
            IsParsing = true;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.RunningWithCancellation);

            await Rifts.InitializeRiftsDB(token);

            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
        }
        catch (TypeInitializationException ex)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"Error occured, make sure you meet requirements of this parser! {ex.Message}", Logger.LogTags.Error);
        }
        catch (OperationCanceledException)
        {
            LogsWindowViewModel.Instance.AddLog("Parsing was canceled by the user.", Logger.LogTags.Warning);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Warning);
        }
        catch (Exception ex)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"{ex.Message}", Logger.LogTags.Error);
        }
        finally
        {
            IsParsing = false;
        }
    }

    private async Task ParseCharacters(CancellationToken token)
    {
        try
        {
            IsParsing = true;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.RunningWithCancellation);

            await Characters.InitializeCharactersDB(token);

            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
        }
        catch (TypeInitializationException ex)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"Error occured, make sure you meet requirements of this parser! {ex.Message}", Logger.LogTags.Error);
        }
        catch (OperationCanceledException)
        {
            LogsWindowViewModel.Instance.AddLog("Parsing was canceled by the user.", Logger.LogTags.Warning);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Warning);
        }
        catch (Exception ex)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"{ex.Message}", Logger.LogTags.Error);
        }
        finally
        {
            IsParsing = false;
        }
    }

    private async Task ParseCosmetics(CancellationToken token)
    {
        try
        {
            IsParsing = true;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.RunningWithCancellation);

            await Cosmetics.InitializeCosmeticsDB(token);

            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
        }
        catch (TypeInitializationException ex)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"Error occured, make sure you meet requirements of this parser! {ex.Message}", Logger.LogTags.Error);
        }
        catch (OperationCanceledException)
        {
            LogsWindowViewModel.Instance.AddLog("Parsing was canceled by the user.", Logger.LogTags.Warning);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Warning);
        }
        catch (Exception ex)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"{ex.Message}", Logger.LogTags.Error);
        }
        finally
        {
            IsParsing = false;
        }
    }

    private async Task ParsePerks(CancellationToken token)
    {
        try
        {
            IsParsing = true;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.RunningWithCancellation);

            await Perks.InitializePerksDB(token);

            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
        }
        catch (TypeInitializationException ex)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"Error occured, make sure you meet requirements of this parser! {ex.Message}", Logger.LogTags.Error);
        }
        catch (OperationCanceledException)
        {
            LogsWindowViewModel.Instance.AddLog("Parsing was canceled by the user.", Logger.LogTags.Warning);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Warning);
        }
        catch (Exception ex)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"{ex.Message}", Logger.LogTags.Error);
        }
        finally
        {
            IsParsing = false;
        }
    }

    private async Task ParseCharacterClasses(CancellationToken token)
    {
        try
        {
            IsParsing = true;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.RunningWithCancellation);

            await CharacterClasses.InitializeCharacterClassesDB(token);

            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
        }
        catch (TypeInitializationException ex)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"Error occured, make sure you meet requirements of this parser! {ex.Message}", Logger.LogTags.Error);
        }
        catch (OperationCanceledException)
        {
            LogsWindowViewModel.Instance.AddLog("Parsing was canceled by the user.", Logger.LogTags.Warning);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Warning);
        }
        catch (Exception ex)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"{ex.Message}", Logger.LogTags.Error);
        }
        finally
        {
            IsParsing = false;
        }
    }

    private async Task ParseTomes(CancellationToken token)
    {
        try
        {
            IsParsing = true;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.RunningWithCancellation);

            await Tomes.InitializeTomesDB(token);

            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
        }
        catch (TypeInitializationException ex)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"Error occured, make sure you meet requirements of this parser! {ex.Message}", Logger.LogTags.Error);
        }
        catch (OperationCanceledException)
        {
            LogsWindowViewModel.Instance.AddLog("Parsing was canceled by the user.", Logger.LogTags.Warning);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Warning);
        }
        catch (Exception ex)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"{ex.Message}", Logger.LogTags.Error);
        }
        finally
        {
            IsParsing = false;
        }
    }

    private async Task ParseAddons(CancellationToken token)
    {
        try
        {
            IsParsing = true;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.RunningWithCancellation);

            await Addons.InitializeAddonsDB(token);

            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
        }
        catch (TypeInitializationException ex)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"Error occured, make sure you meet requirements of this parser! {ex.Message}", Logger.LogTags.Error);
        }
        catch (OperationCanceledException)
        {
            LogsWindowViewModel.Instance.AddLog("Parsing was canceled by the user.", Logger.LogTags.Warning);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Warning);
        }
        catch (Exception ex)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"{ex.Message}", Logger.LogTags.Error);
        }
        finally
        {
            IsParsing = false;
        }
    }

    private async Task ParseItems(CancellationToken token)
    {
        try
        {
            IsParsing = true;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.RunningWithCancellation);

            await Items.InitializeItemsDB(token);

            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
        }
        catch (TypeInitializationException ex)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"Error occured, make sure you meet requirements of this parser! {ex.Message}", Logger.LogTags.Error);
        }
        catch (OperationCanceledException)
        {
            LogsWindowViewModel.Instance.AddLog("Parsing was canceled by the user.", Logger.LogTags.Warning);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Warning);
        }
        catch (Exception ex)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"{ex.Message}", Logger.LogTags.Error);
        }
        finally
        {
            IsParsing = false;
        }
    }

    private async Task ParseOfferings(CancellationToken token)
    {
        try
        {
            IsParsing = true;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.RunningWithCancellation);

            await Offerings.InitializeOfferingsDB(token);

            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
        }
        catch (TypeInitializationException ex)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"Error occured, make sure you meet requirements of this parser! {ex.Message}", Logger.LogTags.Error);
        }
        catch (OperationCanceledException)
        {
            LogsWindowViewModel.Instance.AddLog("Parsing was canceled by the user.", Logger.LogTags.Warning);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Warning);
        }
        catch (Exception ex)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"{ex.Message}", Logger.LogTags.Error);
        }
        finally
        {
            IsParsing = false;
        }
    }

    private async Task ParseMaps(CancellationToken token)
    {
        try
        {
            IsParsing = true;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.RunningWithCancellation);

            await Maps.InitializeMapsDB(token);

            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
        }
        catch (TypeInitializationException ex)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"Error occured, make sure you meet requirements of this parser! {ex.Message}", Logger.LogTags.Error);
        }
        catch (OperationCanceledException)
        {
            LogsWindowViewModel.Instance.AddLog("Parsing was canceled by the user.", Logger.LogTags.Warning);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Warning);
        }
        catch (Exception ex)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"{ex.Message}", Logger.LogTags.Error);
        }
        finally
        {
            IsParsing = false;
        }
    }

    private async Task ParseDlcs(CancellationToken token)
    {
        try
        {
            IsParsing = true;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.RunningWithCancellation);

            await DLCs.InitializeDlcsDB(token);

            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
        }
        catch (TypeInitializationException ex)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"Error occured, make sure you meet requirements of this parser! {ex.Message}", Logger.LogTags.Error);
        }
        catch (OperationCanceledException)
        {
            LogsWindowViewModel.Instance.AddLog("Parsing was canceled by the user.", Logger.LogTags.Warning);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Warning);
        }
        catch (Exception ex)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"{ex.Message}", Logger.LogTags.Error);
        }
        finally
        {
            IsParsing = false;
        }
    }

    private async Task ParseJournals(CancellationToken token)
    {
        try
        {
            IsParsing = true;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.RunningWithCancellation);

            await Journals.InitializeJournalsDB(token);

            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
        }
        catch (TypeInitializationException ex)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"Error occured, make sure you meet requirements of this parser! {ex.Message}", Logger.LogTags.Error);
        }
        catch (OperationCanceledException)
        {
            LogsWindowViewModel.Instance.AddLog("Parsing was canceled by the user.", Logger.LogTags.Warning);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Warning);
        }
        catch (Exception ex)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"{ex.Message}", Logger.LogTags.Error);
        }
        finally
        {
            IsParsing = false;
        }
    }

    private async Task ParseSpecialEvents(CancellationToken token)
    {
        try
        {
            IsParsing = true;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.RunningWithCancellation);

            await SpecialEvents.InitializeSpecialEventsDB(token);

            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
        }
        catch (TypeInitializationException ex)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"Error occured, make sure you meet requirements of this parser! {ex.Message}", Logger.LogTags.Error);
        }
        catch (OperationCanceledException)
        {
            LogsWindowViewModel.Instance.AddLog("Parsing was canceled by the user.", Logger.LogTags.Warning);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Warning);
        }
        catch (Exception ex)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"{ex.Message}", Logger.LogTags.Error);
        }
        finally
        {
            IsParsing = false;
        }
    }

    private async Task ParseCollections(CancellationToken token)
    {
        try
        {
            IsParsing = true;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.RunningWithCancellation);

            await Collections.InitializeCollectionsDB(token);

            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
        }
        catch (TypeInitializationException ex)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"Error occured, make sure you meet requirements of this parser! {ex.Message}", Logger.LogTags.Error);
        }
        catch (OperationCanceledException)
        {
            LogsWindowViewModel.Instance.AddLog("Parsing was canceled by the user.", Logger.LogTags.Warning);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Warning);
        }
        catch (Exception ex)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"{ex.Message}", Logger.LogTags.Error);
        }
        finally
        {
            IsParsing = false;
        }
    }

    private async Task ParseBundles(CancellationToken token)
    {
        try
        {
            IsParsing = true;
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.RunningWithCancellation);

            await Bundles.InitializeBundlesDB(token);

            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
        }
        catch (TypeInitializationException ex)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"Error occured, make sure you meet requirements of this parser! {ex.Message}", Logger.LogTags.Error);
        }
        catch (OperationCanceledException)
        {
            LogsWindowViewModel.Instance.AddLog("Parsing was canceled by the user.", Logger.LogTags.Warning);
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Warning);
        }
        catch (Exception ex)
        {
            LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            LogsWindowViewModel.Instance.AddLog($"{ex.Message}", Logger.LogTags.Error);
        }
        finally
        {
            IsParsing = false;
        }
    }
}