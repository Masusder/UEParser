using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using UEParser.ViewModels;

namespace UEParser.Views;

public partial class DownloadRegisterView : Window
{
    public DownloadRegisterView()
    {
        InitializeComponent();
        DataContext = new DownloadRegisterViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is DownloadRegisterViewModel viewModel)
        {
            var listBox = sender as ListBox;
            var selectedItems = listBox?.SelectedItems?.Cast<string>().ToList();

            if (selectedItems == null) return;

            viewModel.UpdateSelectedRegisters(selectedItems);
        }
    }
}