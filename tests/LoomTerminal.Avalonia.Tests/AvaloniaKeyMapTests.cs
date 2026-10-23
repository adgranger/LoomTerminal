using Avalonia.Input;
using LoomTerminal.Avalonia.Input;

namespace LoomTerminal.Avalonia.Tests;

public sealed class AvaloniaKeyMapTests
{
    [Theory]
    [InlineData(Key.A, 65)]
    [InlineData(Key.Z, 90)]
    [InlineData(Key.D0, 48)]
    [InlineData(Key.D9, 57)]
    [InlineData(Key.NumPad0, 96)]
    [InlineData(Key.NumPad9, 105)]
    [InlineData(Key.F1, 112)]
    [InlineData(Key.F12, 123)]
    [InlineData(Key.F24, 135)]
    [InlineData(Key.Back, 8)]
    [InlineData(Key.Tab, 9)]
    [InlineData(Key.Enter, 13)]
    [InlineData(Key.Escape, 27)]
    [InlineData(Key.Space, 32)]
    [InlineData(Key.PageUp, 33)]
    [InlineData(Key.PageDown, 34)]
    [InlineData(Key.End, 35)]
    [InlineData(Key.Home, 36)]
    [InlineData(Key.Left, 37)]
    [InlineData(Key.Up, 38)]
    [InlineData(Key.Right, 39)]
    [InlineData(Key.Down, 40)]
    [InlineData(Key.Insert, 45)]
    [InlineData(Key.Delete, 46)]
    [InlineData(Key.Multiply, 106)]
    [InlineData(Key.Add, 107)]
    [InlineData(Key.Subtract, 109)]
    [InlineData(Key.Decimal, 110)]
    [InlineData(Key.Divide, 111)]
    [InlineData(Key.OemSemicolon, 186)]
    [InlineData(Key.OemPlus, 187)]
    [InlineData(Key.OemComma, 188)]
    [InlineData(Key.OemMinus, 189)]
    [InlineData(Key.OemPeriod, 190)]
    [InlineData(Key.OemQuestion, 191)]
    [InlineData(Key.OemTilde, 192)]
    [InlineData(Key.OemOpenBrackets, 219)]
    [InlineData(Key.OemPipe, 220)]
    [InlineData(Key.OemCloseBrackets, 221)]
    [InlineData(Key.OemQuotes, 222)]
    public void Key_maps_to_its_dom_key_code(Key key, int keyCode) => Assert.Equal(keyCode, AvaloniaKeyMap.ToKeyCode(key));

    [Fact]
    public void Modifier_keys_map_to_the_same_code_on_both_sides()
    {
        Assert.Equal(16, AvaloniaKeyMap.ToKeyCode(Key.LeftShift));
        Assert.Equal(16, AvaloniaKeyMap.ToKeyCode(Key.RightShift));
        Assert.Equal(17, AvaloniaKeyMap.ToKeyCode(Key.LeftCtrl));
        Assert.Equal(17, AvaloniaKeyMap.ToKeyCode(Key.RightCtrl));
        Assert.Equal(18, AvaloniaKeyMap.ToKeyCode(Key.LeftAlt));
        Assert.Equal(18, AvaloniaKeyMap.ToKeyCode(Key.RightAlt));
    }

    [Fact]
    public void Unknown_key_has_no_key_code() => Assert.Equal(0, AvaloniaKeyMap.ToKeyCode(Key.None));

    [Theory]
    [InlineData(Key.Up, "ArrowUp")]
    [InlineData(Key.Enter, "Enter")]
    [InlineData(Key.F5, "F5")]
    [InlineData(Key.RightAlt, "Alt")]
    [InlineData(Key.LWin, "Meta")]
    public void Non_printable_key_gets_its_dom_name(Key key, string name) =>
        Assert.Equal(name, AvaloniaKeyMap.ToDomKey(key, keySymbol: "x", shift: false));

    [Fact]
    public void Printable_key_uses_the_layout_symbol() =>
        Assert.Equal("é", AvaloniaKeyMap.ToDomKey(Key.D2, "é", shift: false));

    [Fact]
    public void Control_character_symbol_falls_back_to_the_letter() =>
        Assert.Equal("c", AvaloniaKeyMap.ToDomKey(Key.C, "\u0003", shift: false));

    [Fact]
    public void Dead_key_gives_an_empty_key() =>
        Assert.Equal(string.Empty, AvaloniaKeyMap.ToDomKey(Key.OemOpenBrackets, null, shift: false));

    [Theory]
    [InlineData(Key.Tab, true)]
    [InlineData(Key.Left, true)]
    [InlineData(Key.A, false)]
    public void Only_tab_and_arrows_are_navigation_keys(Key key, bool expected) =>
        Assert.Equal(expected, AvaloniaKeyMap.IsNavigationKey(key));
}
