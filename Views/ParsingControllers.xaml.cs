using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using UEParser.ViewModels;
using System.Text.Json.Serialization;
using Avalonia.Controls.Primitives;

namespace UEParser.Views;

public partial class ParsingControllers : UserControl
{

    public ParsingControllers()
    {
        InitializeComponent();
        DataContext = ParsingControllersViewModel.Instance;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

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