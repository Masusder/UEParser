using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ReactiveUI;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using UEParser.Services;
using UEParser.Views;
using Avalonia;
using System.Web;
using System.Collections.Generic;

namespace UEParser.ViewModels;

public class SettingsViewModel : INotifyPropertyChanged
{
    private string? _pathToGameDirectory;

    //public string? PathToGameDirectory
    //{
    //    get { return _pathToGameDirectory; }
    //    set
    //    {
    //        if (_pathToGameDirectory != value)
    //        {
    //            _pathToGameDirectory = value;
    //            OnPropertyChanged(nameof(PathToGameDirectory));
    //        }
    //    }
    //}

    public string? PathToGameDirectory
    {
        get => _pathToGameDirectory;
        set => SetProperty(ref _pathToGameDirectory, value);
    }

    public ICommand? OpenDirectoryDialogCommand { get; }
    public ICommand? SaveSettingsCommand { get; }

    public SettingsViewModel()
    {
        var config = ConfigurationService.Config;
        PathToGameDirectory = config.Core.PathToGameDirectory;
        OpenDirectoryDialogCommand = ReactiveCommand.CreateFromTask<string>(OpenDirectoryDialog);
        SaveSettingsCommand = ReactiveCommand.CreateFromTask(SaveSettings);
    }

    private async Task SaveSettings()
    {
        bool userConfirmedRestart = await ShowRestartPopup();
        if (userConfirmedRestart)
        {
            var config = ConfigurationService.Config;
            config.Core.PathToGameDirectory = PathToGameDirectory ?? "";
            await ConfigurationService.SaveConfiguration();
            RestartApplication();
        }
    }

    private static void RestartApplication()
    {
        (Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow?.Close();

        // Start a new instance of the application
        System.Diagnostics.Process.Start(Environment.ProcessPath ?? throw new Exception());

        Environment.Exit(0);
    }


    private static async Task<bool> ShowRestartPopup()
    {
        var viewModel = new RestartApplicationPopupViewModel();
        var view = new RestartApplicationPopup
        {
            DataContext = viewModel
        };
        var tcs = new TaskCompletionSource<bool>();

        view.Closed += (sender, e) =>
        {
            bool userConfirmedRestart = viewModel.UserConfirmedRestart;
            tcs.SetResult(userConfirmedRestart);
        };

        view.Show();

        bool userConfirmedRestart = viewModel.UserConfirmedRestart;

        return await tcs.Task;
    }

    // TODO: make this command reusable
    private async Task OpenDirectoryDialog(string propertyName)
    {
        var window = new Window();

        var storage = window.StorageProvider;

        var result = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions { AllowMultiple = false});
        if (result.Count > 0)
        {
            string selectedDirectoryPath = result[0].Path.AbsolutePath;
            string decodedPath = HttpUtility.UrlDecode(selectedDirectoryPath);

            //PathToGameDirectory = decodedPath;

            UpdateProperty(propertyName, decodedPath);
        }
    }

    private void UpdateProperty(string propertyName, string path)
    {
        var property = GetType().GetProperty(propertyName);
        if (property != null && property.PropertyType == typeof(string))
        {
            property.SetValue(this, path);
            OnPropertyChanged(propertyName);
        }
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}