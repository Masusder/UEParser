using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using UEParser.AssetRegistry;
using UEParser.Network;

namespace UEParser.ViewModels;

public class DownloadRegisterViewModel : ReactiveObject
{
    private ObservableCollection<string> _selectedRegisters = [];
    private bool _isFetchingRegisters;

    public ObservableCollection<string>? Registers { get; set; }

    public ObservableCollection<string> SelectedRegisters
    {
        get => _selectedRegisters;
        private set
        {
            this.RaiseAndSetIfChanged(ref _selectedRegisters, value);
            this.RaisePropertyChanged(nameof(CanDownloadSelectedRegisters));
        }
    }

    public bool IsFetchingRegisters
    {
        get => _isFetchingRegisters;
        set => this.RaiseAndSetIfChanged(ref _isFetchingRegisters, value);
    }

    public bool CanDownloadSelectedRegisters => SelectedRegisters.Count > 0;

    public ICommand DownloadSelectedRegistersCommand { get; }

    public DownloadRegisterViewModel()
    {
        Registers = [];
        DownloadSelectedRegistersCommand = ReactiveCommand.Create(DownloadSelectedRegisters);

        _selectedRegisters.CollectionChanged += (s, e) =>
        {
            this.RaisePropertyChanged(nameof(CanDownloadSelectedRegisters));
        };

        // Start fetching registers in the background
        FetchRegistersAsync();
    }

    private async Task DownloadSelectedRegisters()
    {
        if (SelectedRegisters.Count == 0)
            return;

        LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Running);
        LogsWindowViewModel.Instance.AddLog($"Downloading the selected file registries.", Logger.LogTags.Info);

        foreach (var register in SelectedRegisters)
        {
            // Registries are stored and downloaded from DBDInfo cloud storage
            string registerName = $"Core_{register}_FilesRegister.uinfo";
            string downloadUrl = GlobalVariables.dbdinfoBaseUrl + $"UEParser/{register}/{registerName}";

            try
            {
                byte[] fileBytes = await API.FetchFileBytesAsync(downloadUrl);

                File.WriteAllBytes(Path.Combine(GlobalVariables.rootDir, "Dependencies", "FilesRegister", registerName), fileBytes);
                LogsWindowViewModel.Instance.AddLog($"Downloaded file registry for {register} version.", Logger.LogTags.Success);
            }
            catch (Exception ex)
            {
                LogsWindowViewModel.Instance.AddLog($"Error downloading {registerName}: {ex.Message}", Logger.LogTags.Error);
                LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Error);
            }
            finally
            {
                LogsWindowViewModel.Instance.ChangeLogState(LogsWindowViewModel.ELogState.Finished);
            }
        }
    }

    public void UpdateSelectedRegisters(List<string> newSelectedItems)
    {
        foreach (var item in newSelectedItems)
        {
            if (!SelectedRegisters.Remove(item))
            {
                SelectedRegisters.Add(item);
            }
        }
    }

    private async void FetchRegistersAsync()
    {
        IsFetchingRegisters = true;
        try
        {
            var registers = await FetchAvailableRegisters();
            Registers?.Clear();

            var localFileRegistersList = FilesRegister.GrabAvailableComparisonVersions();
            string curentVersion = Helpers.ConstructVersionHeaderWithBranch();

            foreach (var register in registers)
            {
                if (!localFileRegistersList.Contains(register) && register != curentVersion)
                {
                    Registers?.Add(register);
                }
            }
        }
        finally
        {
            IsFetchingRegisters = false;
        }
    }

    private static async Task<string[]> FetchAvailableRegisters()
    {
        // Get available registries manifest from DBDInfo API
        string manifestUrl = GlobalVariables.dbdinfoBaseUrl + "api/file-register-manifest";
        var response = await API.FetchUrl(manifestUrl);

        if (!response.Success)
        {
            return [];
        }

        var jsonData = JObject.Parse(response.Data);
        if (jsonData["status"]?.ToString() == "success")
        {
            return jsonData["data"]?.ToObject<string[]>() ?? [];
        }

        return [];
    }
}