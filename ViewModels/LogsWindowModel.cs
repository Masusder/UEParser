using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UEParser.ViewModels;

public class LogsWindowModel : INotifyPropertyChanged
{
    private static LogsWindowModel? _instance;
    public static LogsWindowModel Instance => _instance ??= new LogsWindowModel();

    private string? _logText;

    public string? LogText
    {
        get { return _logText; }
        set
        {
            _logText = value;
            OnPropertyChanged(nameof(LogText));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}