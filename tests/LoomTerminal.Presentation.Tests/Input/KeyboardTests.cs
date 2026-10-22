// Ported from xtermjs/xterm.js: src/common/input/Keyboard.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Presentation.Input;

namespace LoomTerminal.Presentation.Tests.Input;

public class KeyboardTests
{
    private const string Esc = "\u001b";

    private static string? Eval(
        int keyCode,
        bool alt = false,
        bool ctrl = false,
        bool shift = false,
        bool meta = false,
        string key = "",
        bool applicationCursorMode = false,
        bool isMac = false,
        bool macOptionIsMeta = false)
    {
        var ev = new TerminalKeyEvent(keyCode, key, ctrl, alt, shift, meta, TerminalKeyEventType.KeyDown);
        return Keyboard.EvaluateKeyboardEvent(ev, applicationCursorMode, isMac, macOptionIsMeta).Key;
    }

    // should return the correct escape sequence for unmodified keys
    [Theory]
    [InlineData(8, "\u007f")]
    [InlineData(9, "\t")]
    [InlineData(13, "\r")]
    [InlineData(27, "\u001b")]
    [InlineData(33, "\u001b[5~")]
    [InlineData(34, "\u001b[6~")]
    [InlineData(35, "\u001b[F")]
    [InlineData(36, "\u001b[H")]
    [InlineData(37, "\u001b[D")]
    [InlineData(38, "\u001b[A")]
    [InlineData(39, "\u001b[C")]
    [InlineData(40, "\u001b[B")]
    [InlineData(45, "\u001b[2~")]
    [InlineData(46, "\u001b[3~")]
    [InlineData(112, "\u001bOP")]
    [InlineData(113, "\u001bOQ")]
    [InlineData(114, "\u001bOR")]
    [InlineData(115, "\u001bOS")]
    [InlineData(116, "\u001b[15~")]
    [InlineData(117, "\u001b[17~")]
    [InlineData(118, "\u001b[18~")]
    [InlineData(119, "\u001b[19~")]
    [InlineData(120, "\u001b[20~")]
    [InlineData(121, "\u001b[21~")]
    [InlineData(122, "\u001b[23~")]
    [InlineData(123, "\u001b[24~")]
    public void Unmodified_keys_return_the_correct_escape_sequence(int keyCode, string expected)
    {
        Assert.Equal(expected, Eval(keyCode));
    }

    // should return \x1b[3;5~ for ctrl+delete
    // should return \x1b[3;2~ for shift+delete
    // should return \x1b[3;3~ for alt+delete
    [Fact]
    public void Modified_delete_carries_the_modifier_parameter()
    {
        Assert.Equal(Esc + "[3;5~", Eval(46, ctrl: true));
        Assert.Equal(Esc + "[3;2~", Eval(46, shift: true));
        Assert.Equal(Esc + "[3;3~", Eval(46, alt: true));
    }

    // should return \x1b\r for alt+enter
    // should return \x1b\x1b for alt+esc
    [Fact]
    public void Alt_enter_and_alt_escape_are_prefixed_with_escape()
    {
        Assert.Equal(Esc + "\r", Eval(13, alt: true));
        Assert.Equal(Esc + Esc, Eval(27, alt: true));
    }

    // should return \x1b[5D for ctrl+left (and right, up, down)
    [Fact]
    public void Ctrl_arrows_carry_the_modifier_parameter()
    {
        Assert.Equal(Esc + "[1;5D", Eval(37, ctrl: true));
        Assert.Equal(Esc + "[1;5C", Eval(39, ctrl: true));
        Assert.Equal(Esc + "[1;5A", Eval(38, ctrl: true));
        Assert.Equal(Esc + "[1;5B", Eval(40, ctrl: true));
    }

    // should return \x08 for ctrl+backspace
    // should return \x1b\x7f for alt+backspace
    // should return \x1b\x08 for ctrl+alt+backspace
    [Fact]
    public void Backspace_with_modifiers()
    {
        Assert.Equal("\u0008", Eval(8, ctrl: true));
        Assert.Equal(Esc + "\u007f", Eval(8, alt: true));
        Assert.Equal(Esc + "\u0008", Eval(8, ctrl: true, alt: true));
    }

    // On non-macOS platforms: alt + arrows, alt+a, alt+space, ctrl+alt+space
    [Fact]
    public void Alt_combinations_on_non_mac_platforms()
    {
        Assert.Equal(Esc + "[1;3D", Eval(37, alt: true, isMac: false));
        Assert.Equal(Esc + "[1;3C", Eval(39, alt: true, isMac: false));
        Assert.Equal(Esc + "[1;3A", Eval(38, alt: true, isMac: false));
        Assert.Equal(Esc + "[1;3B", Eval(40, alt: true, isMac: false));
        Assert.Equal(Esc + "a", Eval(65, alt: true, isMac: false));
        Assert.Equal(Esc + " ", Eval(32, alt: true, isMac: false));
        Assert.Equal(Esc + "\u0000", Eval(32, alt: true, ctrl: true, isMac: false));
    }

    // On macOS platforms: alt + arrows, undefined for alt+a
    [Fact]
    public void Alt_combinations_on_mac_platforms()
    {
        Assert.Equal(Esc + "[1;3D", Eval(37, alt: true, isMac: true));
        Assert.Equal(Esc + "[1;3C", Eval(39, alt: true, isMac: true));
        Assert.Equal(Esc + "[1;3A", Eval(38, alt: true, isMac: true));
        Assert.Equal(Esc + "[1;3B", Eval(40, alt: true, isMac: true));
        Assert.Null(Eval(65, alt: true, isMac: true));
    }

    // with macOptionIsMeta: should return \x1ba for alt+a / \x1b\x1b for alt+enter
    [Fact]
    public void Alt_combinations_with_mac_option_is_meta()
    {
        Assert.Equal(Esc + "a", Eval(65, alt: true, isMac: true, macOptionIsMeta: true));
        Assert.Equal(Esc + "\r", Eval(13, alt: true, isMac: true, macOptionIsMeta: true));
    }

    // should return \x1b[1;3A for alt+up / \x1b[1;3B for alt+down
    [Fact]
    public void Alt_up_and_down_carry_the_modifier_parameter()
    {
        Assert.Equal(Esc + "[1;3A", Eval(38, alt: true));
        Assert.Equal(Esc + "[1;3B", Eval(40, alt: true));
    }

    // should return the correct escape sequence for modified F1-F12 keys
    [Theory]
    [InlineData(112, "[1;{0}P")]
    [InlineData(113, "[1;{0}Q")]
    [InlineData(114, "[1;{0}R")]
    [InlineData(115, "[1;{0}S")]
    [InlineData(116, "[15;{0}~")]
    [InlineData(117, "[17;{0}~")]
    [InlineData(118, "[18;{0}~")]
    [InlineData(119, "[19;{0}~")]
    [InlineData(120, "[20;{0}~")]
    [InlineData(121, "[21;{0}~")]
    [InlineData(122, "[23;{0}~")]
    [InlineData(123, "[24;{0}~")]
    public void Modified_function_keys_carry_the_modifier_parameter(int keyCode, string format)
    {
        Assert.Equal(Esc + format.Replace("{0}", "2", StringComparison.Ordinal), Eval(keyCode, shift: true));
        Assert.Equal(Esc + format.Replace("{0}", "3", StringComparison.Ordinal), Eval(keyCode, alt: true));
        Assert.Equal(Esc + format.Replace("{0}", "5", StringComparison.Ordinal), Eval(keyCode, ctrl: true));
    }

    // should return proper sequence for ctrl+alt+a
    [Fact]
    public void Ctrl_alt_a_returns_escape_then_soh()
    {
        Assert.Equal(Esc + "\u0001", Eval(65, alt: true, ctrl: true));
    }

    // should return proper sequences for alt+0 ... alt+9, alt+; alt+= alt+, alt+- alt+. alt+/ alt+~ alt+[ alt+\ alt+] alt+'
    [Theory]
    [InlineData(48, "0", ")")]
    [InlineData(49, "1", "!")]
    [InlineData(50, "2", "@")]
    [InlineData(51, "3", "#")]
    [InlineData(52, "4", "$")]
    [InlineData(53, "5", "%")]
    [InlineData(54, "6", "^")]
    [InlineData(55, "7", "&")]
    [InlineData(56, "8", "*")]
    [InlineData(57, "9", "(")]
    [InlineData(186, ";", ":")]
    [InlineData(187, "=", "+")]
    [InlineData(188, ",", "<")]
    [InlineData(189, "-", "_")]
    [InlineData(190, ".", ">")]
    [InlineData(191, "/", "?")]
    [InlineData(192, "`", "~")]
    [InlineData(219, "[", "{")]
    [InlineData(220, "\\", "|")]
    [InlineData(221, "]", "}")]
    [InlineData(222, "'", "\"")]
    public void Alt_digits_and_special_characters(int keyCode, string plain, string shifted)
    {
        Assert.Equal(Esc + plain, Eval(keyCode, alt: true, shift: false));
        Assert.Equal(Esc + shifted, Eval(keyCode, alt: true, shift: true));
    }

    // should handle mobile arrow events
    [Fact]
    public void Mobile_arrow_events_are_handled()
    {
        Assert.Equal(Esc + "[A", Eval(0, key: "UIKeyInputUpArrow"));
        Assert.Equal(Esc + "OA", Eval(0, key: "UIKeyInputUpArrow", applicationCursorMode: true));
        Assert.Equal(Esc + "[D", Eval(0, key: "UIKeyInputLeftArrow"));
        Assert.Equal(Esc + "OD", Eval(0, key: "UIKeyInputLeftArrow", applicationCursorMode: true));
        Assert.Equal(Esc + "[C", Eval(0, key: "UIKeyInputRightArrow"));
        Assert.Equal(Esc + "OC", Eval(0, key: "UIKeyInputRightArrow", applicationCursorMode: true));
        Assert.Equal(Esc + "[B", Eval(0, key: "UIKeyInputDownArrow"));
        Assert.Equal(Esc + "OB", Eval(0, key: "UIKeyInputDownArrow", applicationCursorMode: true));
    }

    // should handle lowercase letters
    [Fact]
    public void Lowercase_letters_are_passed_through()
    {
        Assert.Equal("a", Eval(65, key: "a"));
        Assert.Equal("-", Eval(189, key: "-"));
    }

    // should handle uppercase letters
    [Fact]
    public void Uppercase_letters_are_passed_through()
    {
        Assert.Equal("A", Eval(65, shift: true, key: "A"));
        Assert.Equal("!", Eval(49, shift: true, key: "!"));
    }

    // should return proper sequences for alt+shift+letter combinations
    [Fact]
    public void Alt_shift_letters_are_uppercase_and_alt_letters_lowercase()
    {
        Assert.Equal(Esc + "A", Eval(65, alt: true, shift: true));
        Assert.Equal(Esc + "H", Eval(72, alt: true, shift: true));
        Assert.Equal(Esc + "Z", Eval(90, alt: true, shift: true));
        Assert.Equal(Esc + "a", Eval(65, alt: true, shift: false));
        Assert.Equal(Esc + "h", Eval(72, alt: true, shift: false));
        Assert.Equal(Esc + "z", Eval(90, alt: true, shift: false));
    }

    // should return proper sequence for ctrl+@
    [Fact]
    public void Ctrl_at_returns_nul()
    {
        Assert.Equal("\u0000", Eval(50, ctrl: true, shift: true, key: "@"));
    }
}
