using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using UEParser.ViewModels;

namespace UEParser.Views;

public partial class ParsingControllers : UserControl
{

    public ParsingControllers()
    {
        InitializeComponent();
        DataContext = ParsingControllersModel.Instance;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    //private void ParseEverything(object sender, RoutedEventArgs e)
    //{
    //    LogsWindow.AddLog("Parsing data..");
    //    LogsWindow.AddLog("Data parsed successfully.");
    //}

    //private void ParseRifts(object sender, RoutedEventArgs e)
    //{
    //    LogsWindow.AddLog("[Rifts] Parsing data..");
    //    LogsWindow.AddLog("[Rifts] Data parsed successfully.");
    //}

    //private void ParseCharacters(object sender, RoutedEventArgs e)
    //{
    //    LogsWindow.AddLog("[Characters] Parsing data..");
    //    LogsWindow.AddLog("[Characters] Data parsed successfully.");
    //}
}