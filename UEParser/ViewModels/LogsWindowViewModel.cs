using Avalonia.Media;
using Avalonia.Threading;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Windows.Input;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace UEParser.ViewModels;

public partial class LogsWindowViewModel : ReactiveObject
{
    private static LogsWindowViewModel? _instance;
    public static LogsWindowViewModel Instance => _instance ??= new LogsWindowViewModel();

    // Define the maximum number of log entries
    // too many entries hurt performance
    private const int MaxLogEntries = 250;

    public ICommand ClearLogsCommand { get; }
    public ICommand OpenOutputCommand { get; }
    public bool IsInfoBarOpen { get; private set; }

    private ELogState _logState = ELogState.Neutral;

    public enum ELogState
    {
        Neutral,
        Finished,
        Running,
        RunningWithCancellation,
        Cancellation,
        Error,
        Warning
    }

    public ELogState LogState
    {
        get => _logState;
        set => this.RaiseAndSetIfChanged(ref _logState, value);
    }

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
        OpenOutputCommand = ReactiveCommand.Create(OpenOutput);
        _stateColor = this.WhenAnyValue(x => x.LogState)
                .Select(state => state switch
                {
                    ELogState.Finished => "GreenYellow",
                    ELogState.Running => "Orange",
                    ELogState.RunningWithCancellation => "Orange",
                    ELogState.Cancellation => "#F08080",
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
                    ELogState.RunningWithCancellation => "Tasks Running [ESC to cancel]..",
                    ELogState.Cancellation => "Task cancellation in progress..",
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
                    ELogState.RunningWithCancellation => "Black",
                    ELogState.Cancellation => "Black",
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
                ELogState.RunningWithCancellation => true,
                ELogState.Cancellation => true,
                _ => false
            })
            .ToProperty(this, x => x.IsLoading);
    }

    public ObservableCollection<LogEntry> LogEntries { get; set; } = [];

    public void ChangeLogState(ELogState newState)
    {
        LogState = newState;
    }

    private static void OpenOutput()
    {
        string outputFolder = Path.Combine(GlobalVariables.rootDir, "Output");
        Directory.CreateDirectory(outputFolder);

        // Check if the output folder exists before attempting to open it
        if (Directory.Exists(outputFolder))
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                // Open the folder in file explorer
                ProcessStartInfo startInfo = new()
                {
                    Arguments = outputFolder,
                    FileName = "explorer.exe"
                };
                Process.Start(startInfo);
            });
        }
    }

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

    public void AddLog(string logMessage, Logger.LogTags tag, Logger.ELogExtraTag extraTag = Logger.ELogExtraTag.None)
    {
        // Marshal the UI update to the UI thread
        Dispatcher.UIThread.Post(() =>
        {
            var lines = logMessage.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            foreach (var line in lines)
            {
                // Parse each line of the log entry separately
                LogEntry logEntry = ParseLogEntry(line, tag, extraTag);

                // Update the UI
                LogEntries.Add(logEntry);
            }

            // Truncate the log entries if they exceed the maximum limit
            if (LogEntries.Count > MaxLogEntries)
            {
                LogEntries.RemoveAt(0);
            }

            this.RaisePropertyChanged(nameof(LogEntries));
        });

        // Save the log to file on a background thread
        Task.Run(() => Logger.SaveLog(logMessage, tag, extraTag));
    }

    public void UpdateLog(string logMessage, Logger.LogTags tag, Logger.ELogExtraTag extraTag = Logger.ELogExtraTag.None)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var existingLogEntry = LogEntries.LastOrDefault();
            if (existingLogEntry != null)
            {
                existingLogEntry.Segments.Clear();
                foreach (var segment in ParseLogEntry(logMessage, tag, extraTag).Segments)
                {
                    existingLogEntry.Segments.Add(segment);
                }
                this.RaisePropertyChanged(nameof(LogEntries));
            }
            else
            {
                AddLog(logMessage, tag);
            }
        });
    }

    private static LogEntry ParseLogEntry(string logMessage, Logger.LogTags tag, Logger.ELogExtraTag extraTag)
    {
        var logEntry = new LogEntry();

        string stringTag = $"[{tag.ToString().ToUpper()}]: ";
        Color tagColor = GetTagColor(tag);

        // Regular expression pattern to match text inside []
        //string pattern = @"\[(.*?)\]";

        // Find all matches of the pattern
        //MatchCollection matches = Regex.Matches(logMessage, pattern);

        // Remove the matched segments from the logMessage
        //string cleanedLogMessage = Regex.Replace(logMessage, pattern, "");

        logEntry.Segments.Add(new LogSegment { Text = stringTag, Color = new SolidColorBrush(tagColor) });

        // Extra tag, mainly for parsing controllers
        if (extraTag != Logger.ELogExtraTag.None)
        {
            logEntry.Segments.Add(new LogSegment
            {
                Text = $"[{extraTag}] ",
                Color = new SolidColorBrush(Colors.AntiqueWhite)
            });
        }
        // Iterate through matches and add them as separate segments
        //foreach (Match match in matches)
        //{
        //    string matchedText = match.Groups[1].Value; // Get text inside []
        //    if (!string.IsNullOrEmpty(matchedText)) // Ensure text isn't empty
        //    {
        //        logEntry.Segments.Add(new LogSegment
        //        {
        //            Text = $"[{matchedText}]",
        //            Color = new SolidColorBrush(Colors.AntiqueWhite)
        //        });
        //    }
        //}

        logEntry.Segments.Add(new LogSegment
        {
            Text = logMessage,
            Color = new SolidColorBrush(Colors.White),
            FontFamily = IsQrCodeSegment(logMessage) ? new FontFamily("Courier New") : new FontFamily("Segoe UI Variable") // Generated QR code needs Courier New font
        });

        return logEntry;
    }

    // Helper method to determine if a text segment is part of the QR code
    private static bool IsQrCodeSegment(string text)
    {
        return text.Contains('█') || text.Contains('▄') || text.Contains('▀');
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
            case Logger.LogTags.Debug:
                return Colors.Orange;
            default:
                return Colors.White;
        }
#pragma warning restore IDE0066
    }
}

public class LogEntry
{
    public ObservableCollection<LogSegment> Segments { get; } = [];
}

public class LogSegment
{
    public required string Text { get; set; }
    public required SolidColorBrush Color { get; set; }
    public FontFamily FontFamily { get; set; } = new FontFamily("Segoe UI Variable"); // Default font
}