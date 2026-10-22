// Ported from xtermjs/xterm.js: src/browser/Clipboard.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Presentation.Input;

/// <summary>Turns pasted text into pty input: line ending normalization and bracketed paste.</summary>
/// <remarks>Threading: stateless; any thread.</remarks>
internal static class PasteEncoding
{
    /// <summary>Normalizes line endings (CRLF and LF become CR).</summary>
    /// <param name="text">The pasted text.</param>
    /// <returns>The text to insert into the terminal.</returns>
    internal static string PrepareTextForTerminal(string text)
    {
        return text.Replace("\r\n", "\r", StringComparison.Ordinal).Replace('\n', '\r');
    }

    /// <summary>Brackets the text for paste when needed (https://cirw.in/blog/bracketed-paste).</summary>
    /// <param name="text">The pasted text.</param>
    /// <param name="bracketedPasteMode">Whether the application enabled bracketed paste.</param>
    /// <returns>The text, bracketed or not.</returns>
    internal static string BracketTextForPaste(string text, bool bracketedPasteMode)
    {
        return bracketedPasteMode ? "\u001b[200~" + text + "\u001b[201~" : text;
    }

    /// <summary>The <c>paste</c> logic: prepare, then bracket unless bracketed paste is off or ignored.</summary>
    /// <param name="text">The pasted text.</param>
    /// <param name="bracketedPasteMode">Whether the application enabled bracketed paste.</param>
    /// <param name="ignoreBracketedPasteMode">The <c>ignoreBracketedPasteMode</c> option.</param>
    /// <returns>The bytes to send, as a string.</returns>
    internal static string PreparePaste(string text, bool bracketedPasteMode, bool ignoreBracketedPasteMode)
    {
        return BracketTextForPaste(PrepareTextForTerminal(text), bracketedPasteMode && !ignoreBracketedPasteMode);
    }
}
