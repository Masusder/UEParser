using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UEParser.Services;
using UEParser.Views;

namespace UEParser.ViewModels;

public class APIModel
{
    private string _version = "";

    public string Version
    {
        get => _version;
        set
        {
            if (_version != value)
            {
                _version = value;
                OnPropertyChanged();
            }
        }
    }

    public APIModel()
    {
        ConstructFullVersion();
    }

    public void ConstructFullVersion()
    {
        var config = ConfigurationService.Config;
        string? versionHeader = config?.Core.VersionData.LatestVersion;
        string? branch = config?.Core.VersionData.Branch.ToString();

        string fullVersion = $"Selected version: {versionHeader}_{branch}";
        Version = fullVersion;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}