using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace UEParser.Views;

public partial class Home : UserControl
{
    public Home()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}