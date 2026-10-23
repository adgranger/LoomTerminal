using Avalonia.Automation.Peers;
using Avalonia.Threading;
using LoomTerminal.Avalonia.Accessibility;

namespace LoomTerminal.Avalonia.Controls;

public sealed partial class TerminalView
{
    // Avalonia creates the peer only when a platform automation client asks for it: that is the "a screen reader
    // listens" signal. Text capture costs the parser thread, so it stays off until then.
    private bool _accessibilityRequested;

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        _accessibilityRequested = true;
        if (_session is { } session)
        {
            session.AccessibilityEnabled = true;
        }

        return new TerminalViewAutomationPeer(this);
    }

    // Raised on the parser thread: marshal before touching the peer.
    private void OnSessionTextAnnounced(string text) => Dispatcher.UIThread.Post(() =>
    {
        if (ControlAutomationPeer.FromElement(this) is TerminalViewAutomationPeer peer)
        {
            peer.Announce(text);
        }
    });
}
