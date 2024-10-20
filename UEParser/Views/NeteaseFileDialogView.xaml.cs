using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using UEParser.Models.Netease;
using UEParser.ViewModels;

namespace UEParser.Views;

public partial class NeteaseFileDialogView : Window
{
    public NeteaseFileDialogViewModel ViewModel { get; private set; }

    public NeteaseFileDialogView()
    {
        InitializeComponent();
        ViewModel = new NeteaseFileDialogViewModel();
        DataContext = ViewModel;
        ViewModel.CloseAction += HandleClose;
    }

    private void HandleClose(bool result)
    {
        Close(result);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void Load(IEnumerable<ManifestFileData> files, string version)
    {
        ViewModel.LoadFiles(files);
        ViewModel.LoadVersion(version);
    }

    private void OnCancelButtonClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}