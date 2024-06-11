//using Avalonia;
//using Avalonia.Controls;
//using Avalonia.Markup.Xaml;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace UEParser.Views;

//public partial class LogsWindow : UserControl
//{
//    public static readonly StyledProperty<string> LogTextProperty =
//        AvaloniaProperty.Register<LogsWindow, string>(nameof(LogText), defaultValue: "");

//    public LogsWindow()
//    {
//        InitializeComponent();
//    }

//    private void InitializeComponent()
//    {
//        AvaloniaXamlLoader.Load(this);
//    }

//    private void LogText_TextChanged(object sender, TextChangedEventArgs e)
//    {
//        // Scroll to the end of the text box
//        if (sender is TextBox textBox)
//        {
//            if (!string.IsNullOrEmpty(textBox.Text))
//            {
//                textBox.CaretIndex = textBox.Text.Length;
//            }
//        }
//    }
//}

using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia;
using UEParser.ViewModels;
using Avalonia.VisualTree;

namespace UEParser.Views
{
    public partial class LogsWindow : UserControl
    {
        public LogsWindow()
        {
            InitializeComponent();
            DataContext = LogsWindowModel.Instance;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public static void AddLog(string log)
        {
            LogsWindowModel.Instance.LogText += log + "\n";
        }

        public static void ClearLogs()
        {
            LogsWindowModel.Instance.LogText = "";
        }

        private void LogText_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Scroll to the end of the text box
            if (sender is TextBox textBox)
            {
                if (!string.IsNullOrEmpty(textBox.Text))
                {
                    var scrollViewer = textBox.FindAncestorOfType<ScrollViewer>();
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
    }
}