using Avalonia.Markup.Xaml;
using UEParser.ViewModels;
using Avalonia.Controls;

namespace UEParser.Views;

public partial class AssetsExtractorView : UserControl
{
    public AssetsExtractorView()
    {
        InitializeComponent();
        DataContext = new AssetsExtractorViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
