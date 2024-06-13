using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia;
using UEParser.ViewModels;
using Avalonia.VisualTree;
using Avalonia.Controls.Documents;

namespace UEParser.Views
{
    public partial class LogsWindow : UserControl
    {
        public LogsWindow()
        {
            InitializeComponent();
            DataContext = LogsWindowModel.Instance;

            var viewModel = (LogsWindowModel)DataContext;
            viewModel.AddLog("[INFO]: This is an informational message.");
            viewModel.AddLog("[WARN]: This is a warning message.");
            viewModel.AddLog("[ERROR]: This is an error message.");

            viewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(LogsWindowModel.LogEntries))
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
            var viewModel = (LogsWindowModel?)DataContext;
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


        //private void LogText_TextChanged(object sender, TextChangedEventArgs e)
        //{
        //    // Scroll to the end of the text box (only if scroll already is at the very end of the text box)
        //    if (sender is TextBox textBox)
        //    {
        //        if (!string.IsNullOrEmpty(textBox.Text))
        //        {
        //            var scrollViewer = textBox.FindAncestorOfType<ScrollViewer>();
        //            if (scrollViewer != null)
        //            {
        //                if (scrollViewer.Offset.Y + scrollViewer.Viewport.Height >= scrollViewer.Extent.Height)
        //                {
        //                    scrollViewer.ScrollToEnd();
        //                }
        //            }
        //        }
        //    }
        //}
    }
}