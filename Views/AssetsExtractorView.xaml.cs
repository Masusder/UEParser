﻿using Avalonia.Markup.Xaml;
using UEParser.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace UEParser.Views;

public partial class AssetsExtractorView : UserControl
{
    public AssetsExtractorView()
    {
        InitializeComponent();
        DataContext = new AssetsExtractorViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnPointerExit(object sender, PointerEventArgs e)
    {
        if (sender is Button button)
        {
            button.Background = new SolidColorBrush(Colors.DodgerBlue);
        }
    }
}
