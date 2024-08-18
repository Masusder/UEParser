using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Interactivity;
using Avalonia.Animation.Easings;
using Avalonia.Threading;
using System;
using System.Threading.Tasks;
using UEParser.ViewModels;
using UEParser.Services;
using UEParser.AssetRegistry;

namespace UEParser.Views;

public partial class LogsWindowView : UserControl
{
    public LogsWindowView()
    {
        InitializeComponent();
        DataContext = LogsWindowViewModel.Instance;

        var config = ConfigurationService.Config;
        string versionWithBranch = GlobalVariables.versionWithBranch;
        string comparisonVersionWithBranch = GlobalVariables.compareVersionWithBranch;
        string buildVersionNumber = string.IsNullOrEmpty(config.Core.BuildVersionNumber) ? "---" : config.Core.BuildVersionNumber;

        if (string.IsNullOrEmpty(versionWithBranch) || versionWithBranch.StartsWith('_'))
        {
            versionWithBranch = "---";
        }

        if (string.IsNullOrEmpty(comparisonVersionWithBranch) || comparisonVersionWithBranch.StartsWith('_'))
        {
            comparisonVersionWithBranch = "---";
        }

        var viewModel = (LogsWindowViewModel)DataContext;
        viewModel.AddLog("UEParser started.", Logger.LogTags.Info);
        viewModel.AddLog($"Current core build version: {buildVersionNumber}", Logger.LogTags.Info);
        viewModel.AddLog($"Current version: {versionWithBranch}", Logger.LogTags.Info);
        viewModel.AddLog($"Comparison version: {comparisonVersionWithBranch}", Logger.LogTags.Info);

        bool isComparedVersionValid = FilesRegister.DoesComparedRegisterExist();

        if (!isComparedVersionValid && comparisonVersionWithBranch != "---")
        {
            viewModel.AddLog("Not found files register for configured comparison version! In order to use provided comparison version you need to initialize app with that version beforehand.", Logger.LogTags.Warning);
        }

        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(LogsWindowViewModel.LogEntries))
            {
                UpdateLogText();
            }
        };

        UpdateLogText();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void UpdateLogText()
    {
        var viewModel = (LogsWindowViewModel?)DataContext;
        var logTextBlock = this.FindControl<SelectableTextBlock>("LogTextBlock") ?? new SelectableTextBlock();

        logTextBlock.GotFocus += (sender, e) =>
        {
            e.Handled = true;
        };

        if (viewModel == null) return;

        logTextBlock?.Inlines?.Clear();

        foreach (var logEntry in viewModel.LogEntries)
        {
            foreach (var segment in logEntry.Segments)
            {
                var run = new Run
                {
                    Text = segment.Text,
                    Foreground = segment.Color
                };
                logTextBlock?.Inlines?.Add(run);
            }
            logTextBlock?.Inlines?.Add(new LineBreak());
        }

        ScrollLogToEnd();
    }

    // Scroll to the end of the text box (only if scroll already is at the very end of the text box)
    private void ScrollLogToEnd()
    {
        var scrollViewer = this.FindControl<ScrollViewer>("LogScrollViewer");
        if (scrollViewer != null)
        {
            if (scrollViewer.Offset.Y + scrollViewer.Viewport.Height >= scrollViewer.Extent.Height)
            {
                scrollViewer.ScrollToEnd();
            }
        }
    }

    public void ScrollToTop(object sender, RoutedEventArgs args)
    {
        var scrollViewer = this.FindControl<ScrollViewer>("LogScrollViewer");
        if (scrollViewer != null)
        {
            if (scrollViewer.Offset.Y + scrollViewer.Viewport.Height >= scrollViewer.Extent.Height)
            {
                SmoothScrollToOffset(scrollViewer, new Vector(0, 0), TimeSpan.FromSeconds(0.5));
            }
        }
    }

    public void ScrollToBottom(object sender, RoutedEventArgs args)
    {
        var scrollViewer = this.FindControl<ScrollViewer>("LogScrollViewer");
        if (scrollViewer != null)
        {
            if (scrollViewer.Offset.Y + scrollViewer.Viewport.Height < scrollViewer.Extent.Height)
            {
                var targetOffset = new Vector(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);
                SmoothScrollToOffset(scrollViewer, targetOffset, TimeSpan.FromSeconds(0.5));
            }
        }
    }

    private static async void SmoothScrollToOffset(ScrollViewer scrollViewer, Vector targetOffset, TimeSpan duration)
    {
        var startOffset = scrollViewer.Offset;
        var easing = new CubicEaseOut();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        while (stopwatch.Elapsed < duration)
        {
            var t = stopwatch.Elapsed.TotalMilliseconds / duration.TotalMilliseconds;
            t = Math.Clamp(t, 0, 1);

            var easedT = easing.Ease(t);
            var newOffset = new Vector(
                Interpolate(startOffset.X, targetOffset.X, easedT),
                Interpolate(startOffset.Y, targetOffset.Y, easedT)
            );

            scrollViewer.Offset = newOffset;

            // Wait for the next frame
            await Dispatcher.UIThread.InvokeAsync(() => Task.Delay(16));
        }

        // Ensure final position is set
        scrollViewer.Offset = targetOffset;
    }

    private static double Interpolate(double start, double end, double t)
    {
        return start + (end - start) * t;
    }

    public class CubicEaseOut : Easing
    {
        public override double Ease(double progress)
        {
            var p = progress - 1;
            return p * p * p + 1;
        }
    }
}