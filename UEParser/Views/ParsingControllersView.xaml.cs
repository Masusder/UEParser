using System;
using System.Linq;
using System.Text.Json.Serialization;
using Avalonia.Input;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using UEParser.ViewModels;

using Avalonia;

namespace UEParser.Views;

public partial class ParsingControllersView : UserControl
{

    public ParsingControllersView()
    {
        InitializeComponent();
        DataContext = ParsingControllersViewModel.Instance;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private static readonly char[] Separator = [';'];
    private void Button_PointerEnter(object sender, PointerEventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        var descriptionText = this.FindControl<TextBlock>("DescriptionText") ?? new TextBlock();
        var popup = this.FindControl<Popup>("TextPopup") ?? new Popup();
        var border = this.FindControl<Border>("TextBorder") ?? new Border();
        descriptionText.Inlines?.Clear();

        // Retrieve ButtonInfo from Button.Resources
        if (button.Resources.TryGetValue("ButtonInfo", out object? buttonInfoObj) && buttonInfoObj is StringResources buttonInfo)
        {
            // Add Title with <Run> styling
            if (!string.IsNullOrEmpty(buttonInfo.Title))
            {
                descriptionText.Inlines?.Add(new Run { Text = buttonInfo.Title, FontWeight = Avalonia.Media.FontWeight.Bold });
                descriptionText.Inlines?.Add(new LineBreak());
            }

            // Add Description
            if (!string.IsNullOrEmpty(buttonInfo.Description))
            {
                descriptionText.Inlines?.Add(new Run { Text = buttonInfo.Description });
            }

            // Add Requirements
            if (buttonInfo.Requirements != null && buttonInfo.Requirements.Length != 0)
            {
                descriptionText.Inlines?.Add(new TextBlock { Margin = new Thickness(0, 10, 0, 0) }); // Adjust top margin as needed
                descriptionText.Inlines?.Add(new LineBreak());
                descriptionText.Inlines?.Add(new Run { Text = "Requirements:", FontWeight = Avalonia.Media.FontWeight.Bold, FontSize = 12 });
                descriptionText.Inlines?.Add(new LineBreak());

                // Split requirements by ;
                var requirementsArray = buttonInfo.Requirements.Split(Separator, StringSplitOptions.RemoveEmptyEntries);

                // Add each requirement
                foreach (var requirement in requirementsArray)
                {
                    descriptionText.Inlines?.Add(new Run { Text = $"• {requirement.Trim()}", Foreground = Avalonia.Media.Brushes.AntiqueWhite, FontSize = 12 });
                    descriptionText.Inlines?.Add(new LineBreak());
                }

                // Remove the last comma or separator
                if (descriptionText.Inlines?.Count > 0)
                {
                    descriptionText.Inlines?.Remove(descriptionText.Inlines.Last());
                }
            }
        }

        popup.IsOpen = true;
        popup.Opacity = 1;
        border.Opacity = 1;
    }

    private void Button_PointerLeave(object sender, PointerEventArgs e)
    {
        var popup = this.FindControl<Popup>("TextPopup") ?? new Popup();
        var border = this.FindControl<Border>("TextBorder") ?? new Border();
        popup.IsOpen = false;
        popup.Opacity = 0;
        border.Opacity = 0;
    }

    private class ButtonTag
    {
        [JsonPropertyName("Title")]
        public required string Title { get; set; }

        [JsonPropertyName("Description")]
        public required string Description { get; set; }
    }
}