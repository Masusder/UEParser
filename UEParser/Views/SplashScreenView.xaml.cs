using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace UEParser.Views;

public partial class SplashScreenView : UserControl
{
    public SplashScreenView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}