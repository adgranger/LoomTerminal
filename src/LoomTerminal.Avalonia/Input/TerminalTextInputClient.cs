using Avalonia;
using Avalonia.Input.TextInput;

namespace LoomTerminal.Avalonia.Input;

/// <summary>
/// Types with an IME (and the emoji panel) through Avalonia's <see cref="TextInputMethodClient"/>. The terminal is not
/// a text document: the surrounding text is empty and only the pre-edit string is tracked, drawn as an overlay at the
/// cursor. Nothing is sent to the shell until the platform commits through <c>TextInput</c> (xterm.js
/// <c>CompositionHelper</c>).
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class TerminalTextInputClient(
    Visual view, Func<Rect> getCursorRectangle, Action<string?> compositionChanged) : TextInputMethodClient
{
    /// <summary>Whether a composition is active; keys belong to the IME meanwhile.</summary>
    public bool IsComposing { get; private set; }

    /// <inheritdoc />
    public override Visual TextViewVisual => view;

    /// <inheritdoc />
    public override bool SupportsPreedit => true;

    /// <inheritdoc />
    public override bool SupportsSurroundingText => false;

    /// <inheritdoc />
    public override string SurroundingText => string.Empty;

    /// <inheritdoc />
    public override Rect CursorRectangle => getCursorRectangle();

    /// <inheritdoc />
    public override TextSelection Selection { get; set; }

    /// <inheritdoc />
    public override void SetPreeditText(string? preeditText) => SetPreeditText(preeditText, null);

    /// <inheritdoc />
    public override void SetPreeditText(string? preeditText, int? cursorPos)
    {
        IsComposing = !string.IsNullOrEmpty(preeditText);
        compositionChanged(IsComposing ? preeditText : null);
    }

    /// <summary>Tells the platform that the cursor cell moved, so the candidate window follows.</summary>
    public void NotifyCursorMoved() => RaiseCursorRectangleChanged();

    /// <summary>Drops an abandoned composition (focus lost): it is cleared, not committed.</summary>
    public void Reset() => SetPreeditText(null, null);
}
