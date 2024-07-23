using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UEParser.ViewModels;

namespace UEParser.Views;

public partial class UpdateManagerView : UserControl
{
    public UpdateManagerView()
    {
        InitializeComponent();
        DataContext = new UpdateManagerViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}