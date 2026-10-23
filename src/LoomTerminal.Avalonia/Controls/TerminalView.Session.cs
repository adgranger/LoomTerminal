using Avalonia.Threading;
using LoomTerminal.Presentation.Session;

namespace LoomTerminal.Avalonia.Controls;

public sealed partial class TerminalView
{
    private TerminalSession? _session;

    /// <summary>Raised on the UI thread when the shell changes the window title.</summary>
    public event EventHandler<string>? TitleChanged;

    /// <summary>Raised on the UI thread when the shell exited; the argument is the exit code.</summary>
    public event EventHandler<int>? Exited;

    /// <summary>Raised on the UI thread when the application rings the bell.</summary>
    public event EventHandler? BellRang;

    /// <summary>
    /// The session shown by the control. Not a styled property: it is not bindable data. Setting it detaches the
    /// previous session without disposing it (the host owns sessions).
    /// </summary>
    public TerminalSession? Session
    {
        get => _session;
        set
        {
            if (ReferenceEquals(_session, value))
            {
                return;
            }

            if (_session is { } old)
            {
                old.TitleChanged -= OnSessionTitleChanged;
                old.Exited -= OnSessionExited;
                old.Bell -= OnSessionBell;
                old.FrameAvailable -= OnSessionFrameAvailable;
            }

            _session = value;
            (_columns, _rows) = (0, 0);
            RequestFrame();

            if (value is not null)
            {
                value.TitleChanged += OnSessionTitleChanged;
                value.Exited += OnSessionExited;
                value.Bell += OnSessionBell;
                value.FrameAvailable += OnSessionFrameAvailable;
                // Settings made before the session was attached (the host sets them first) reach it now.
                value.SetTheme(TerminalTheme);
                value.SetCursorStyle(CursorStyle);
                value.SetCursorBlink(CursorBlink);
                value.SetFocus(IsFocused);
            }
        }
    }

    // Session events come from the parser thread: marshal them before raising ours.
    private void OnSessionFrameAvailable() => Dispatcher.UIThread.Post(RequestFrame);

    private void OnSessionTitleChanged(string title) => Dispatcher.UIThread.Post(() => TitleChanged?.Invoke(this, title));

    private void OnSessionExited(int exitCode) => Dispatcher.UIThread.Post(() => Exited?.Invoke(this, exitCode));

    private void OnSessionBell() => Dispatcher.UIThread.Post(() => BellRang?.Invoke(this, EventArgs.Empty));
}
