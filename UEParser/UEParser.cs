using Avalonia;
using Avalonia.ReactiveUI;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;
using System;
using System.IO;

namespace UEParser;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        SetNativeLibraryPath();
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        IconProvider.Current
            .Register<FontAwesomeIconProvider>();

        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();
    }

    // Allow dependent DLLs to be contained in .data subfolder
    private static void SetNativeLibraryPath()
    {
        var currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var nativeLibraryPath = Path.Combine(currentDirectory, ".data");

        if (Directory.Exists(nativeLibraryPath))
        {
            // Hide directory
            DirectoryInfo dirInfo = new(nativeLibraryPath);
            dirInfo.Attributes |= FileAttributes.Hidden;

            var path = Environment.GetEnvironmentVariable("PATH");
            path = $"{nativeLibraryPath}{Path.PathSeparator}{path}";
            Environment.SetEnvironmentVariable("PATH", path);
        }
    }
}
