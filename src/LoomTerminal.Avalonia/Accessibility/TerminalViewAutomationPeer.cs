using Avalonia.Automation;
using Avalonia.Automation.Peers;
using LoomTerminal.Avalonia.Controls;

namespace LoomTerminal.Avalonia.Accessibility;

/// <summary>
/// Describes the terminal to UI Automation: a <c>Document</c> that announces new output as a polite live region.
/// </summary>
/// <remarks>
/// Avalonia 12 has no text pattern (no <c>ITextProvider</c>), so unlike the WinUI peer this one cannot expose the
/// visible lines for line navigation: screen readers get the name and the live announcements only.
/// Threading: UI thread only.
/// </remarks>
internal sealed class TerminalViewAutomationPeer(TerminalView owner) : ControlAutomationPeer(owner)
{
    private const string DefaultName = "Terminal";

    private string? _announcement;

    protected override string GetClassNameCore() => nameof(TerminalView);

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Document;

    protected override AutomationLiveSetting GetLiveSettingCore() => AutomationLiveSetting.Polite;

    // The name of a live region is what a screen reader reads when it changes: it carries the last announcement.
    protected override string? GetNameCore() => _announcement ?? base.GetNameCore() ?? DefaultName;

    /// <summary>Tells screen readers about new output.</summary>
    /// <param name="text">The text to read.</param>
    internal void Announce(string text)
    {
        var old = _announcement;
        _announcement = text;
        RaisePropertyChangedEvent(AutomationElementIdentifiers.NameProperty, old, text);
    }
}
