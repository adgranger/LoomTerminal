using LoomTerminal.Presentation.Input;
using LoomTerminal.Presentation.Links;
using LoomTerminal.Presentation.Session;
using Microsoft.UI.Dispatching;

namespace LoomTerminal.WinUI.Controls;

public sealed partial class TerminalControl : ILinkHandler
{
    private TerminalSession? _session;

    /// <summary>Raised on the UI thread when the shell changes the window title.</summary>
    public event EventHandler<string>? TitleChanged;

    /// <summary>Raised on the UI thread when the shell exited; the argument is the exit code.</summary>
    public event EventHandler<int>? Exited;

    /// <summary>Raised on the UI thread when the application rings the bell.</summary>
    public event EventHandler? BellRang;

    /// <summary>Raised before a key is handled by the terminal; set <see cref="KeyPreviewEventArgs.Handled"/> to claim it.</summary>
    public event EventHandler<KeyPreviewEventArgs>? KeyPreview;

    /// <summary>Raised when the user activates a link; when not handled the control does nothing, the host opens it.</summary>
    public event EventHandler<LinkActivatedEventArgs>? LinkActivated;

    /// <summary>
    /// The session shown by the control. Not a dependency property: it is not bindable data. Setting it detaches the
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
                old.KeyPreview = null;
                old.LinkHandler = null;
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
                value.KeyPreview = OnSessionKeyPreview;
                value.LinkHandler = this;
            }
        }
    }

    // Session events come from the parser thread: marshal them before raising ours.
    private void OnSessionTitleChanged(string title) => DispatcherQueue.TryEnqueue(() => TitleChanged?.Invoke(this, title));

    private void OnSessionExited(int exitCode) => DispatcherQueue.TryEnqueue(() => Exited?.Invoke(this, exitCode));

    private void OnSessionBell() => DispatcherQueue.TryEnqueue(() => BellRang?.Invoke(this, EventArgs.Empty));

    // Called on the thread that sends the key, which is the UI thread.
    private bool OnSessionKeyPreview(TerminalKeyEvent key)
    {
        var args = new KeyPreviewEventArgs(key);
        KeyPreview?.Invoke(this, args);
        return args.Handled;
    }

    bool ILinkHandler.CanOpen(Uri uri) => true;

    void ILinkHandler.Open(Uri uri) => LinkActivated?.Invoke(this, new LinkActivatedEventArgs(uri));
}

/// <summary>Data of <see cref="TerminalControl.LinkActivated"/>.</summary>
/// <remarks>Threading: raised and used on the UI thread.</remarks>
public sealed class LinkActivatedEventArgs : EventArgs
{
    /// <summary>Creates the arguments.</summary>
    /// <param name="uri">The activated link.</param>
    public LinkActivatedEventArgs(Uri uri) => Uri = uri;

    /// <summary>The activated link.</summary>
    public Uri Uri { get; }

    /// <summary>Whether the host handled the link (the control does nothing either way).</summary>
    public bool Handled { get; set; }
}
