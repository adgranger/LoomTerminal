using LoomTerminal.Presentation.Session;
using LoomTerminal.Pty;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace LoomTerminal.PackageTest;

/// <summary>The only window of the package test app: one terminal running the first detected shell.</summary>
/// <remarks>Threading: UI thread only.</remarks>
public sealed partial class MainWindow : Window
{
    private readonly TerminalSession _session;

    /// <summary>Creates the window and starts the shell.</summary>
    public MainWindow()
    {
        InitializeComponent();
        string commandLine = Shells.Detect() is [var first, ..] ? first.CommandLine : "cmd.exe";
        _session = TerminalSession.Start(new PtyStartOptions(commandLine));
        Terminal.Session = _session;
        Closed += (_, _) =>
        {
            Terminal.Session = null;
            _session.Dispose();
        };
        // A missing style does not crash by itself: turn it into a crash log the smoke test sees.
        Terminal.Loaded += (_, _) =>
        {
            if (VisualTreeHelper.GetChildrenCount(Terminal) == 0)
            {
                throw new InvalidOperationException(
                    "The TerminalControl template was not applied: Generic.xaml is missing from the package.");
            }
        };
    }
}
