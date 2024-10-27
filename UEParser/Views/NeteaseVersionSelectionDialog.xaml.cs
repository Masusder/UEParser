using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace UEParser.Views;

public partial class NeteaseVersionSelectionDialog : Window
{
    private string? SelectedVersion { get; set; }

    private NeteaseVersionSelectionDialog(string[] versions)
    {
        InitializeComponent();
        
        var versionComboBox = this.FindControl<ComboBox>("VersionComboBox") ?? throw new InvalidOperationException("VersionComboBox not found in the dialog.");;
        var okButton = this.FindControl<Button>("OkButton") ?? throw new InvalidOperationException("OkButton not found in the dialog.");
        var cancelButton = this.FindControl<Button>("CancelButton") ?? throw new InvalidOperationException("CancelButton not found in the dialog.");
        
        versionComboBox.ItemsSource = versions;
        okButton.Click += (_, _) => { SelectedVersion = (string)versionComboBox.SelectedItem!; Close(); };
        cancelButton.Click += (_, _) => { SelectedVersion = null; Close(); };
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public static async Task<string?> ShowDialogCustom(string[] versions)
    {
        var dialog = new NeteaseVersionSelectionDialog(versions);
        var tcs = new TaskCompletionSource<string?>();
        
        dialog.Closed += (_, _) =>
        {
            tcs.SetResult(dialog.SelectedVersion);
        };
        
        dialog.Show();

        return await tcs.Task;
    }
}