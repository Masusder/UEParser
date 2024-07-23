using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using UEParser.Views;

namespace UEParser.ViewModels;

public class MainWindowViewModel : INotifyPropertyChanged
{
    private object _selectedCategory = "Home";
    private Control _currentPage = new Home();

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
        CurrentPage = new Home();
    }

    private void SetCurrentPage()
    {
        if (SelectedCategory is NavigationViewItem nvi)
        {
            switch (nvi?.Tag?.ToString())
            {
                case "Home":
                    CurrentPage = new Home();
                    break;
                case "Controllers":
                    CurrentPage = new ParsingControllers();
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
                    CurrentPage = new Home();
                    break;
            }
        }
    }

    private static void OpenSettingsWindow()
    {
        var settingsWindow = new Settings();
        settingsWindow.Show();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}