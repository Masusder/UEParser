using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using UEParser.ViewModels;

namespace UEParser.Views;

public partial class ParsingControlers : UserControl
{

    public ParsingControlers()
    {
        InitializeComponent();
        DataContext = LogsWindowModel.Instance;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void ParseButton1_Click(object sender, RoutedEventArgs e)
    {
        LogsWindow.AddLog("[Cosmetics] Parsing data..");
        LogsWindow.AddLog("[Cosmetics] Data parsed successfully.");
    }

    private void ParseButton2_Click(object sender, RoutedEventArgs e)
    {
        LogsWindow.AddLog("[Items] Parsing data..");
        LogsWindow.AddLog("[Items] Data parsed successfully.");
    }

    private void ParseButton3_Click(object sender, RoutedEventArgs e)
    {
        LogsWindow.AddLog("[Rifts] Parsing data..");
        LogsWindow.AddLog("[Rifts] Data parsed successfully.");
    }
}