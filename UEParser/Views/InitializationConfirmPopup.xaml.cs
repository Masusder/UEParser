using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using UEParser.ViewModels;

namespace UEParser.Views;

public partial class InitializationConfirmPopup : Window
{
    private readonly InitializationConfirmPopupViewModel _viewModel;

    public InitializationConfirmPopup()
    {
        InitializeComponent();
        _viewModel = new InitializationConfirmPopupViewModel();
        DataContext = _viewModel;

        // Subscribe to OnClose event to handle popup closing
        _viewModel.CloseAction += HandleClose;
    }

    private void HandleClose(bool result)
    {
        // Close the window with a result
        Close(result);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
