using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using UEParser.ViewModels;

namespace UEParser.Views;

public partial class Settings : Window
{
    public Settings()
    {
        InitializeComponent();
        DataContext = new SettingsViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}