// Inspired by LoomEditor: src/LoomEditor.WinUI/Accessibility/LoomEditorAutomationPeer.cs
using LoomTerminal.WinUI.Controls;
using Microsoft.UI.Xaml.Automation.Peers;

namespace LoomTerminal.WinUI.Accessibility;

/// <summary>Describes the terminal to UI Automation: a read-only <c>Document</c> exposing the visible text.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed partial class TerminalAutomationPeer : FrameworkElementAutomationPeer
{
    private readonly TerminalTextProvider _text;

    public TerminalAutomationPeer(TerminalControl owner)
        : base(owner)
    {
        _text = new TerminalTextProvider(owner, () => ProviderFromPeer(this));
    }

    protected override string GetClassNameCore() => nameof(TerminalControl);

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Document;

    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        return string.IsNullOrEmpty(name) ? "Terminal" : name;
    }

    protected override object? GetPatternCore(PatternInterface patternInterface)
        => patternInterface == PatternInterface.Text ? _text : base.GetPatternCore(patternInterface);

    /// <summary>Whether a UI Automation client listens for notifications (a screen reader is running).</summary>
    internal static bool HasListener => ListenerExists(AutomationEvents.LiveRegionChanged);

    /// <summary>Tells screen readers about new output.</summary>
    /// <param name="text">The text to read.</param>
    internal void Announce(string text)
        => RaiseNotificationEvent(AutomationNotificationKind.Other, AutomationNotificationProcessing.ImportantAll, text, "LoomTerminalOutput");
}
