using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia;
using UEParser.ViewModels;
using Avalonia.VisualTree;
using Avalonia.Controls.Documents;
using Avalonia.Input;

namespace UEParser.Views
{
    public partial class LogsWindow : UserControl
    {
        public LogsWindow()
        {
            InitializeComponent();
            DataContext = LogsWindowViewModel.Instance;

            var viewModel = (LogsWindowViewModel)DataContext;
            viewModel.AddLog("UEParser initialized.", Logger.LogTags.Info);

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
            var logTextBlock = this.FindControl<SelectableTextBlock>("LogTextBlock");

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
    }
}