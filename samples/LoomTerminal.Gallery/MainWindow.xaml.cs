using LoomTerminal.Presentation.Session;
using LoomTerminal.Presentation.Theming;
using LoomTerminal.Pty;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace LoomTerminal.Gallery;

/// <summary>The main window of the gallery.</summary>
public sealed partial class MainWindow : Window
{
    /// <summary>Creates the window.</summary>
    public MainWindow()
    {
        InitializeComponent();

        // The first detected shell, or cmd.exe; the window owns the session and disposes it when it closes.
        string commandLine = Shells.Detect() is [var first, ..] ? first.CommandLine : "cmd.exe";
        var session = TerminalSession.Start(new PtyStartOptions(commandLine));
        Terminal.Session = session;
        Closed += (_, _) =>
        {
            Terminal.Session = null;
            session.Dispose();
        };
    }

    // The control does not follow the app theme itself: the host sets Theme.
    private void OnThemeToggled(object sender, RoutedEventArgs e) =>
        Terminal.Theme = LightTheme.IsOn
            ? new TerminalTheme { Foreground = "#1f1f1f", Background = "#ffffff", Cursor = "#1f1f1f", SelectionBackground = "#add6ff" }
            : new TerminalTheme { Foreground = "#cccccc", Background = "#1e1e1e", Cursor = "#aeafad", SelectionBackground = "#264f78" };

    private void OnFontSizeChanged(object sender, RangeBaseValueChangedEventArgs e) => Terminal.FontSize = e.NewValue;

    private void OnLineHeightChanged(object sender, RangeBaseValueChangedEventArgs e) => Terminal.LineHeight = e.NewValue;
}
