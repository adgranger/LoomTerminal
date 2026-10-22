using LoomTerminal.Presentation.Session;
using LoomTerminal.Pty;
using Microsoft.UI.Xaml;

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
}
