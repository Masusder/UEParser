using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using UEParser.ViewModels;
using FluentAvalonia.UI.Windowing;
using Avalonia.Media;
using System.Threading.Tasks;
using System.Threading;
using System;

using UEParser.Views;

namespace UEParser;

public partial class MainWindow : AppWindow
{
    public MainWindow()
    {
        InitializeComponent();
        SplashScreen = new MainAppSplashScreen();
        DataContext = new MainWindowViewModel();
    }

    internal class MainAppSplashScreen() : IApplicationSplashScreen
    {
        public string? AppName { get; }
        public IImage? AppIcon { get; }
        public object SplashScreenContent => new SplashScreen();
        public int MinimumShowTime => 2000;

        public Action? InitApp { get; set; }

        public Task RunTasks(CancellationToken cancellationToken)
        {
            if (InitApp == null)
                return Task.CompletedTask;

            return Task.Run(InitApp, cancellationToken);
        }
    }
}