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
            DataContext = LogsWindowModel.Instance;

            var viewModel = (LogsWindowModel)DataContext;
            viewModel.AddLog("[INFO]: Lorem ipsum dolor sit amet, consectetur adipiscing elit. Morbi tempor orci quis mi viverra cursus. Etiam vel feugiat sapien. Donec interdum erat dolor, vel varius tortor ornare at. Aenean sed mi augue. Pellentesque risus erat, maximus id risus in, aliquet venenatis arcu. Vestibulum cursus mauris sit amet tortor consequat dapibus. Suspendisse nec purus tellus. Praesent sed tincidunt massa. Fusce id sem eget turpis pulvinar pulvinar.");
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
    }
}