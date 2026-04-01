using Avalonia;
using PuantajApp.Services;
using System;

namespace PuantajApp;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        EnvService.Load(".env");
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
