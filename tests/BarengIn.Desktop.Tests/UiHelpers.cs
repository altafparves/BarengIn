using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace BarengIn.Desktop.Tests;

public static class UiHelpers
{
    public static T Find<T>(this Window w, string name) where T : Control
    {
        return w.FindControl<T>(name) ?? throw new InvalidOperationException("No control named " + name);
    }

    public static void Click(this Window w, string buttonName)
    {
        w.Find<Button>(buttonName).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
    }

    public static void Press(this Button b)
    {
        b.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
    }

    public static void Layout(this Window w)
    {
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        w.UpdateLayout();
    }

    public static Button FindButton(this Window w, string content)
    {
        return w.GetVisualDescendants().OfType<Button>().First(b => (b.Content as string) == content && b.IsEffectivelyVisible);
    }
}
