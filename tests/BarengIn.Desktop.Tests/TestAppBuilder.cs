using Avalonia;
using Avalonia.Headless;
using BarengIn.Desktop;

[assembly: AvaloniaTestApplication(typeof(BarengIn.Desktop.Tests.TestAppBuilder))]

namespace BarengIn.Desktop.Tests;

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .WithInterFont()
        .UseSkia()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
