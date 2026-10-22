// Ported from xtermjs/xterm.js: src/browser/Clipboard.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Presentation.Input;

namespace LoomTerminal.Presentation.Tests.Input;

public class PasteEncodingTests
{
    // should replace carriage return and/or line feed with carriage return
    [Fact]
    public void Line_endings_are_replaced_with_carriage_return()
    {
        Assert.Equal("foo\rbar\r", PasteEncoding.PrepareTextForTerminal("foo\nbar\n"));
        Assert.Equal("foo\rbar\r", PasteEncoding.PrepareTextForTerminal("foo\r\nbar\r\n"));
    }

    // should bracket pasted text in bracketedPasteMode
    [Fact]
    public void Text_is_bracketed_in_bracketed_paste_mode()
    {
        Assert.Equal("foo bar", PasteEncoding.BracketTextForPaste("foo bar", false));
        Assert.Equal("\u001b[200~foo bar\u001b[201~", PasteEncoding.BracketTextForPaste("foo bar", true));
    }

    [Fact]
    public void Paste_normalizes_then_brackets_when_the_mode_is_on()
    {
        Assert.Equal("\u001b[200~a\rb\u001b[201~", PasteEncoding.PreparePaste("a\r\nb", true, false));
    }

    [Fact]
    public void Paste_is_not_bracketed_when_the_mode_is_off_or_ignored()
    {
        Assert.Equal("a\rb", PasteEncoding.PreparePaste("a\nb", false, false));
        Assert.Equal("a\rb", PasteEncoding.PreparePaste("a\nb", true, true));
    }
}
