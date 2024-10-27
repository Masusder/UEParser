using System;
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
    private SettingsView? _settingsWindow;

    public object SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (value is NavigationViewItem nvi && nvi.Tag?.ToString() == "Settings")
            {
                // Handle Settings separately and don't allow multiple Setting views to be open
                OpenSettingsWindow();
            }
            else
            {
                _selectedCategory = value;
                OnPropertyChanged();
                SetCurrentPage();
            }
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
            CurrentPage = (nvi?.Tag?.ToString()) switch
            {
                "Home" => new HomeView(),
                "Controllers" => new ParsingControllersView(),
                "WebsiteUpdate" => new UpdateManagerView(),
                "API" => new APIView(),
                "AssetsExtractor" => new AssetsExtractorView(),
                "Netease" => new NeteaseView(),
                _ => new HomeView(),
            };
        }
    }

    private void OpenSettingsWindow()
    {
        if (_settingsWindow is not { IsVisible: true })
        {
            _settingsWindow = new SettingsView();
            _settingsWindow.Closed += OnSettingsWindowClosed;
            _settingsWindow.Show();
        }
        else
        {
            _settingsWindow.Activate();
        }
    }

    private void OnSettingsWindowClosed(object? sender, EventArgs e)
    {
        _settingsWindow = null;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}