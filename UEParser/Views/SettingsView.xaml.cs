using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using UEParser.ViewModels;

namespace UEParser.Views;

public partial class SettingsView : Window
{
    public SettingsView()
    {
        InitializeComponent();
        DataContext = new SettingsViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void DownloadButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var downloadWindow = new DownloadRegisterView();
        downloadWindow.Show();
    }
}