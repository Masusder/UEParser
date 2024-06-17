using Avalonia.Media;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace UEParser.ViewModels
{
    public static class LogColors
    {
        public static readonly Dictionary<string, Color> TagColors = new()
        {
        { "ERROR", Colors.Red },
        { "INFO", Colors.Blue },
        { "WARN", Colors.Yellow },
        { "SUCCESS", Colors.Green }
    };
    };

    public partial class LogsWindowModel : INotifyPropertyChanged
    {
        private static LogsWindowModel? _instance;
        public static LogsWindowModel Instance => _instance ??= new LogsWindowModel();

        public ICommand? ClearLogsCommand { get; }
        public bool IsInfoBarOpen { get; private set; }

        private LogsWindowModel() 
        {
            ClearLogsCommand = ReactiveCommand.Create(ClearLogs);
        }

        public ObservableCollection<LogEntry> LogEntries { get; set; } = [];

        private void ClearLogs()
        {
            LogEntries.Clear();
            OnPropertyChanged(nameof(LogEntries));
            IsInfoBarOpen = true;
            OnPropertyChanged(nameof(IsInfoBarOpen));
        }

        public void AddLog(string log)
        {
            LogEntry logEntry = ParseLogEntry(log);
            LogEntries.Add(logEntry);
            OnPropertyChanged(nameof(LogEntries));
        }

        [GeneratedRegex(@"(\[ERROR\]|\[INFO\]|\[WARN\]|\[SUCCESS\])")]
        private static partial Regex LogsColorRegex();

        private static readonly Regex TagRegex = LogsColorRegex();

        private static LogEntry ParseLogEntry(string log)
        {
            var logEntry = new LogEntry();
            var matches = TagRegex.Matches(log);
            int currentIndex = 0;

            foreach (Match match in matches)
            {
                // Add preceding text segment (if any) with default color
                if (match.Index > currentIndex)
                {
                    string precedingText = log[currentIndex..match.Index];
                    logEntry.Segments.Add(new LogSegment { Text = precedingText, Color = new SolidColorBrush(Colors.White) });
                }

                // Add tag segment with specific color
                string tag = match.Groups[0].Value;
                Color tagColor = GetTagColor(tag);
                logEntry.Segments.Add(new LogSegment { Text = tag, Color = new SolidColorBrush(tagColor) });

                // Move current index past the tag
                currentIndex = match.Index + match.Length;
            }

            // Add remaining text segment (after last tag) with default color
            if (currentIndex < log.Length)
            {
                string remainingText = log[currentIndex..];
                logEntry.Segments.Add(new LogSegment { Text = remainingText, Color = new SolidColorBrush(Colors.White) });
            }

            return logEntry;
        }

        private static Color GetTagColor(string tag)
        {
#pragma warning disable IDE0066 
            switch (tag)
            {
                case "[ERROR]":
                    return Colors.Red;
                case "[INFO]":
                    return Colors.DodgerBlue;
                case "[WARN]":
                    return Colors.Yellow;
                case "[SUCCESS]":
                    return Colors.GreenYellow;
                default:
                    return Colors.White; // Default color
            }
#pragma warning restore IDE0066
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
    }

}
