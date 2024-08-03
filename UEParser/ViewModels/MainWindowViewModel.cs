using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using UEParser.Views;

namespace UEParser.ViewModels;

public class MainWindowViewModel : INotifyPropertyChanged
{
    private object _selectedCategory = "Home";
    private Control _currentPage = new HomeView();

    public object SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            _selectedCategory = value;
            OnPropertyChanged();
            SetCurrentPage();
        }
    }

    public Control CurrentPage
    {
        get => _currentPage;
        set
        {
            _currentPage = value;
            OnPropertyChanged();
        }
    }

    public MainWindowViewModel()
    {
        // Initialize with Home page
        CurrentPage = new HomeView();
    }

    private void SetCurrentPage()
    {
        if (SelectedCategory is NavigationViewItem nvi)
        {
            switch (nvi?.Tag?.ToString())
            {
                case "Home":
                    CurrentPage = new HomeView();
                    break;
                case "Controllers":
                    CurrentPage = new ParsingControllersView();
                    break;
                case "WebsiteUpdate":
                    CurrentPage = new UpdateManagerView();
                    break;
                case "Settings":
                    OpenSettingsWindow();
                    break;
                case "API":
                    CurrentPage = new APIView();
                    break;
                case "AssetsExtractor":
                    CurrentPage = new AssetsExtractorView();
                    break;
                default:
                    CurrentPage = new HomeView();
                    break;
            }
        }
    }

    private static void OpenSettingsWindow()
    {
        var settingsWindow = new SettingsView();
        settingsWindow.Show();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}