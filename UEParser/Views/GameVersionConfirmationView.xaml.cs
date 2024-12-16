using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using UEParser.ViewModels;

namespace UEParser.Views;

public partial class GameVersionConfirmationView : Window
{
    private readonly GameVersionConfirmationViewModel _viewModel;

    public GameVersionConfirmationView(string detectedVersion)
    {
        InitializeComponent();
        _viewModel = new GameVersionConfirmationViewModel(detectedVersion);
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