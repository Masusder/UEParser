﻿using System;
using System.Windows.Input;
using ReactiveUI;
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

    private void ParseRifts()
    {
        LogsWindowViewModel.Instance.AddLog("[Rifts] Parsing data..", Logger.LogTags.Info);
        LogsWindowViewModel.Instance.AddLog("[Rifts] Data parsed successfully.", Logger.LogTags.Success);
    }

    private void ParseCharacters()
    {
        LogsWindowViewModel.Instance.AddLog("[Characters] Parsing data..", Logger.LogTags.Info);
        LogsWindowViewModel.Instance.AddLog("[Characters] Data parsed successfully.", Logger.LogTags.Success);
    }
}