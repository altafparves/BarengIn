using Avalonia.Controls;
using Avalonia.Layout;

namespace BarengIn.Desktop;

public static class MessageBox
{
    public static Task Show(Window owner, string message)
    {
        var ok = new Button
        {
            Content = "OK",
            Width = 90,
            HorizontalAlignment = HorizontalAlignment.Right,
            Classes = { "primary" },
        };

        var dialog = new Window
        {
            Title = "BarengIn",
            Width = 360,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(24),
                Spacing = 20,
                Children =
                {
                    new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                    ok,
                },
            },
        };

        ok.Click += (_, _) => dialog.Close();
        return dialog.ShowDialog(owner);
    }
}
