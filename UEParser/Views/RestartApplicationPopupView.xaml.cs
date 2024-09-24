using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using UEParser.ViewModels;

namespace UEParser.Views;

public partial class RestartApplicationPopupView : Window
{

    public RestartApplicationPopupView()
    {
        InitializeComponent();
        DataContext = new RestartApplicationPopupViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void YesButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is RestartApplicationPopupViewModel viewModel)
        {
            viewModel.UserConfirmedRestart = true;
        }
        Close();
    }

    private void NoButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is RestartApplicationPopupViewModel viewModel)
        {
            viewModel.UserConfirmedRestart = false;
        }
        Close();
    }
}