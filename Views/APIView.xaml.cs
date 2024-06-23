using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Collections.Generic;
using System.Threading.Tasks;
using UEParser.ViewModels;

namespace UEParser.Views;

public partial class APIView : UserControl
{
    public APIView()
    {
        InitializeComponent();
        DataContext = new APIViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}