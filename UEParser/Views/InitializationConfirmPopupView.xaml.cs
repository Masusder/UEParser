using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using UEParser.ViewModels;

namespace UEParser.Views;

public partial class InitializationConfirmPopupView : Window
{
    private readonly InitializationConfirmPopupViewModel _viewModel;

    public InitializationConfirmPopupView()
    {
        InitializeComponent();
        _viewModel = new InitializationConfirmPopupViewModel();
        DataContext = _viewModel;

        // Subscribe to OnClose event to handle popup closing
        _viewModel.CloseAction += HandleClose;
    }

    private void HandleClose(bool result)
    {
        Close(result);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
