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
        DataContext = ParsingControllersViewModel.Instance;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}