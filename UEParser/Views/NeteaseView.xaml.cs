using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using UEParser.ViewModels;

namespace UEParser.Views;

public partial class NeteaseView : UserControl
{
    public NeteaseView()
    {
        InitializeComponent();
        DataContext = new NeteaseViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
