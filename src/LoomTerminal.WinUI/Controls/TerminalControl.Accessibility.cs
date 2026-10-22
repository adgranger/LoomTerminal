using LoomTerminal.Presentation.Rendering;
using LoomTerminal.WinUI.Accessibility;
using Microsoft.UI.Xaml.Automation.Peers;

namespace LoomTerminal.WinUI.Controls;

public sealed partial class TerminalControl
{
    /// <summary>The last frame drawn, read by the UI Automation text provider.</summary>
    internal RenderFrame? LastFrame => _lastFrame;

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new TerminalAutomationPeer(this);

    // Text capture costs the parser thread: only run it while a UIA client (screen reader) listens.
    private void UpdateAccessibility()
    {
        if (_session is { } session)
        {
            session.AccessibilityEnabled = TerminalAutomationPeer.HasListener;
        }
    }

    // Raised on the parser thread: marshal before touching the peer.
    private void OnSessionTextAnnounced(string text) => DispatcherQueue.TryEnqueue(() =>
    {
        if (FrameworkElementAutomationPeer.FromElement(this) is TerminalAutomationPeer peer)
        {
            peer.Announce(text);
        }
    });
}
