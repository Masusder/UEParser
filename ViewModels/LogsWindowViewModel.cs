﻿using Avalonia.Media;
using Avalonia.Threading;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Windows.Input;
using System;

namespace UEParser.ViewModels;

public partial class LogsWindowViewModel : ReactiveObject
{
    private static LogsWindowViewModel? _instance;
    public static LogsWindowViewModel Instance => _instance ??= new LogsWindowViewModel();

    public ICommand? ClearLogsCommand { get; }
    public bool IsInfoBarOpen { get; private set; }

    private ELogState _logState = ELogState.Neutral;

    public enum ELogState
    {
        Neutral,
        Finished,
        Running,
        Error,
        Warning
    }

    public ELogState LogState
    {
        get => _logState;
        set => this.RaiseAndSetIfChanged(ref _logState, value);
    }

    //private string _stateColor = "#323232";

    //public string StateColor
    //{
    //    get { return _stateColor; }
    //    set
    //    {
    //        if (_stateColor != value)
    //        {
    //            _stateColor = value;
    //            OnPropertyChanged(nameof(StateColor));
    //        }
    //    }
    //}

    private readonly ObservableAsPropertyHelper<string> _stateColor;
    public string StateColor => _stateColor.Value;

    private readonly ObservableAsPropertyHelper<string> _stateText;
    public string StateText => _stateText.Value;

    private readonly ObservableAsPropertyHelper<string> _stateTextColor;
    public string StateTextColor => _stateTextColor.Value;

    private readonly ObservableAsPropertyHelper<bool> _isLoading;
    public bool IsLoading => _isLoading.Value;

    private LogsWindowViewModel()
    {
        ClearLogsCommand = ReactiveCommand.Create(ClearLogs);
        _stateColor = this.WhenAnyValue(x => x.LogState)
                .Select(state => state switch
                {
                    ELogState.Finished => "GreenYellow",
                    ELogState.Running => "Orange",
                    ELogState.Error => "Red",
                    ELogState.Neutral => "#323232",
                    ELogState.Warning => "#ffcc00",
                    _ => "#323232",
                })
                .ToProperty(this, x => x.StateColor);

        _stateText = this.WhenAnyValue(x => x.LogState)
                .Select(state => state switch
                {
                    ELogState.Finished => "Tasks Finished",
                    ELogState.Running => "Tasks Running..",
                    ELogState.Error => "Error Occured",
                    ELogState.Neutral => "Waiting.. do something",
                    ELogState.Warning => "Warning",
                    _ => "Waiting.. do something",
                })
                .ToProperty(this, x => x.StateText);

        _stateTextColor = this.WhenAnyValue(x => x.LogState)
                .Select(state => state switch
                {
                    ELogState.Finished => "Black",
                    ELogState.Running => "Black",
                    ELogState.Error => "White",
                    ELogState.Neutral => "White",
                    ELogState.Warning => "Black",
                    _ => "White",
                })
                .ToProperty(this, x => x.StateTextColor);

        _isLoading = this.WhenAnyValue(x => x.LogState)
            .Select(state => state switch
            {
                ELogState.Running => true,
                _ => false
            })
            .ToProperty(this, x => x.IsLoading);
    }

    public ObservableCollection<LogEntry> LogEntries { get; set; } = [];

    public void ChangeLogState(ELogState newState)
    {
        LogState = newState;
        //SetStateColor();
    }

    //private void SetStateColor()
    //{
    //    StateColor = LogState switch
    //    {
    //        ELogState.Finished => "Green",
    //        ELogState.Running => "Yellow",
    //        ELogState.Error => "Red",
    //        ELogState.Neutral => "#323232",
    //        _ => "#323232",
    //    };
    //}

    //private void ClearLogs()
    //{
    //    LogEntries.Clear();
    //    this.RaisePropertyChanged(nameof(LogEntries));
    //    IsInfoBarOpen = true;
    //    this.RaisePropertyChanged(nameof(IsInfoBarOpen));
    //}

    private async Task ClearLogs()
    {
        LogEntries.Clear();
        this.RaisePropertyChanged(nameof(LogEntries));
        IsInfoBarOpen = true;
        this.RaisePropertyChanged(nameof(IsInfoBarOpen));

        // Automatically close InfoBar after 3 seconds
        await Task.Delay(TimeSpan.FromSeconds(3)).ContinueWith(_ =>
        {
            IsInfoBarOpen = false;
            this.RaisePropertyChanged(nameof(IsInfoBarOpen));
        });
    }

    public void AddLog(string logMessage, Logger.LogTags tag)
    {
        // Marshal the UI update to the UI thread
        Dispatcher.UIThread.Post(() =>
        {
            // Parse the log entry
            LogEntry logEntry = ParseLogEntry(logMessage, tag);

            // Update the UI
            LogEntries.Add(logEntry);
            this.RaisePropertyChanged(nameof(LogEntries));
        });

        // Save the log to file on a background thread
        Task.Run(() => Logger.SaveLog(logMessage, tag));

    }

    private static LogEntry ParseLogEntry(string logMessage, Logger.LogTags tag)
    {
        var logEntry = new LogEntry();

        string stringTag = $"[{tag.ToString().ToUpper()}]: ";

        Color tagColor = GetTagColor(tag);

        logEntry.Segments.Add(new LogSegment { Text = stringTag, Color = new SolidColorBrush(tagColor) });
        logEntry.Segments.Add(new LogSegment { Text = logMessage, Color = new SolidColorBrush(Colors.White) });

        return logEntry;
    }

    private static Color GetTagColor(Logger.LogTags tag)
    {
#pragma warning disable IDE0066
        switch (tag)
        {
            case Logger.LogTags.Error:
                return Colors.Red;
            case Logger.LogTags.Info:
                return Colors.DodgerBlue;
            case Logger.LogTags.Warning:
                return Colors.Yellow;
            case Logger.LogTags.Success:
                return Colors.GreenYellow;
            default:
                return Colors.White;
        }
#pragma warning restore IDE0066
    }

    //public event PropertyChangedEventHandler? PropertyChanged;

    //protected virtual void OnPropertyChanged(string propertyName)
    //{
    //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    //}
}

public class LogEntry
{
    public ObservableCollection<LogSegment> Segments { get; } = [];
}

public class LogSegment
{
    public required string Text { get; set; }
    public required SolidColorBrush Color { get; set; }
}