using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Collections.Generic;
using System.Threading.Tasks;
using UEParser.ViewModels;

namespace UEParser.Views;

public partial class API : UserControl
{
    public API()
    {
        InitializeComponent();
        DataContext = new APIModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}