// Ported from xtermjs/xterm.js: src/common/services/OptionsService.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Services;

namespace LoomTerminal.Core.Tests.Services;

// Not ported: the 'setOption' tests about fontWeight and the cols/rows "undefined" constructor test (UI option and
// non-nullable ints here); the 'object.keys' test (options are a record, not a getter/setter object).
public class OptionsServiceTests
{
    // xterm.js: 'OptionsService' › 'constructor' › 'uses values from constructor option values if correctly passed'
    [Fact]
    public void Constructor_uses_the_given_values_when_they_are_valid()
    {
        OptionsService service = new(TerminalOptions.Default with { Cols = 80, Rows = 25 });
        Assert.Equal(25, service.Options.Rows);
        Assert.Equal(80, service.Options.Cols);
    }

    // xterm.js: 'OptionsService' › 'constructor' › 'uses default value if invalid constructor option value passed'
    [Fact]
    public void Constructor_uses_the_default_when_a_value_is_invalid()
    {
        OptionsService service = new(TerminalOptions.Default with { TabStopWidth = 0 });
        Assert.Equal(TerminalOptions.Default.TabStopWidth, service.Options.TabStopWidth);
    }

    [Fact]
    public void Default_options_match_the_xterm_js_defaults()
    {
        TerminalOptions o = new OptionsService().RawOptions;
        Assert.Equal(80, o.Cols);
        Assert.Equal(24, o.Rows);
        Assert.Equal(1000, o.Scrollback);
        Assert.Equal(8, o.TabStopWidth);
        Assert.Equal("xterm", o.TermName);
        Assert.Equal(" ()[]{}',\"`", o.WordSeparator);
        Assert.True(o.AltClickMovesCursor);
        Assert.False(o.RightClickSelectsWord);
        Assert.Equal(new WindowsPtyOptions(), o.WindowsPty);
    }

    // xterm.js: 'OptionsService' › 'onOptionChange' › 'should fire on any option change'
    [Fact]
    public void OnOptionChange_fires_for_any_option_change_until_disposed()
    {
        OptionsService service = new();
        List<TerminalOption> fired = [];
        IDisposable subscription = service.OnOptionChange.Subscribe(fired.Add);
        service.SetOptions(service.RawOptions with { CursorWidth = 10 });
        Assert.Equal([TerminalOption.CursorWidth], fired);

        subscription.Dispose();
        List<TerminalOption> second = [];
        service.OnOptionChange.Subscribe(second.Add);
        service.SetOptions(service.RawOptions with { Scrollback = 20 });
        Assert.Equal([TerminalOption.Scrollback], second);
        Assert.Equal([TerminalOption.CursorWidth], fired);
    }

    [Fact]
    public void OnOptionChange_fires_once_per_changed_property_in_declaration_order_and_not_for_unchanged_ones()
    {
        OptionsService service = new();
        List<TerminalOption> fired = [];
        service.OnOptionChange.Subscribe(fired.Add);
        service.SetOptions(service.RawOptions with { TermName = "vt", Cols = 100, Scrollback = 1000 });
        Assert.Equal([TerminalOption.Cols, TerminalOption.TermName], fired);
    }

    // xterm.js: 'OptionsService' › 'onSpecificOptionChange' › 'should fire only on a specific option change'
    [Fact]
    public void OnSpecificOptionChange_fires_only_for_that_option_and_sees_the_new_value()
    {
        OptionsService service = new();
        List<long> values = [];
        service.OnSpecificOptionChange(TerminalOption.Scrollback, () => values.Add(service.RawOptions.Scrollback));
        service.SetOptions(service.RawOptions with { CursorWidth = 10 });
        Assert.Empty(values);
        service.SetOptions(service.RawOptions with { Scrollback = 20 });
        Assert.Equal([20L], values);
    }

    // xterm.js: 'OptionsService' › 'onMultipleOptionChange' › 'should fire only for specific options'
    [Fact]
    public void OnMultipleOptionChange_fires_only_for_the_given_options()
    {
        OptionsService service = new();
        int called = 0;
        service.OnMultipleOptionChange([TerminalOption.Scrollback], () => called++);
        service.SetOptions(service.RawOptions with { CursorWidth = 10 });
        Assert.Equal(0, called);
        service.SetOptions(service.RawOptions with { Scrollback = 20 });
        Assert.Equal(1, called);
    }

    // xterm.js: 'OptionsService' › 'constructor' (validation by _sanitizeAndValidateOption)
    [Fact]
    public void SetOptions_throws_with_the_xterm_js_messages_and_changes_nothing()
    {
        OptionsService service = new();
        TerminalOptions before = service.RawOptions;
        Assert.Equal(
            "tabStopWidth cannot be less than 1, value: 0",
            Assert.Throws<ArgumentException>(() => service.SetOptions(before with { TabStopWidth = 0 })).Message);
        Assert.Equal(
            "cursorWidth cannot be less than 1, value: 0",
            Assert.Throws<ArgumentException>(() => service.SetOptions(before with { CursorWidth = 0.5 })).Message);
        Assert.Equal(
            "scrollback cannot be less than 0, value: -1",
            Assert.Throws<ArgumentException>(() => service.SetOptions(before with { Scrollback = -1 })).Message);
        Assert.Equal(
            "scrollSensitivity cannot be less than or equal to 0, value: 0",
            Assert.Throws<ArgumentException>(() => service.SetOptions(before with { ScrollSensitivity = 0 })).Message);
        Assert.Equal(
            "fastScrollSensitivity cannot be less than or equal to 0, value: -2",
            Assert.Throws<ArgumentException>(() => service.SetOptions(before with { FastScrollSensitivity = -2 })).Message);
        Assert.Same(before, service.RawOptions);
    }

    [Fact]
    public void SetOptions_sanitizes_values_instead_of_throwing_where_xterm_js_does()
    {
        OptionsService service = new();
        service.SetOptions(service.RawOptions with { CursorWidth = 2.7, MinimumContrastRatio = 30, WordSeparator = string.Empty });
        Assert.Equal(2, service.RawOptions.CursorWidth);
        Assert.Equal(21, service.RawOptions.MinimumContrastRatio);
        Assert.Equal(TerminalOptions.Default.WordSeparator, service.RawOptions.WordSeparator);

        service.SetOptions(service.RawOptions with { MinimumContrastRatio = 0.2, Scrollback = long.MaxValue, WindowsPty = null });
        Assert.Equal(1, service.RawOptions.MinimumContrastRatio);
        Assert.Equal(4294967295, service.RawOptions.Scrollback);
        Assert.Equal(new WindowsPtyOptions(), service.RawOptions.WindowsPty);

        service.SetOptions(service.RawOptions with { MinimumContrastRatio = 4.26 });
        Assert.Equal(4.3, service.RawOptions.MinimumContrastRatio);
    }

    [Fact]
    public void Disposing_the_service_stops_the_events()
    {
        OptionsService service = new();
        int called = 0;
        service.OnOptionChange.Subscribe(_ => called++);
        service.Dispose();
        service.SetOptions(service.RawOptions with { Cols = 90 });
        Assert.Equal(0, called);
    }
}
