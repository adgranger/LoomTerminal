using LoomTerminal.Core.Services;

namespace LoomTerminal.Core.Tests;

public sealed class WindowsModeTests
{
    private static TerminalCore CreateTerminal(TerminalOptions options) => new(options with { Cols = 80, Rows = 24 });

    private static bool IsSecondLineWrapped(TerminalCore terminal)
    {
        terminal.Write(new string('a', 80));
        terminal.Write("\n");
        return terminal.Buffers.Active.Lines.Get(1)!.IsWrapped;
    }

    [Fact]
    public void An_old_ConPTY_build_marks_the_line_after_a_full_line_as_wrapped()
    {
        using TerminalCore terminal = CreateTerminal(TerminalOptions.Default with { WindowsPty = new WindowsPtyOptions("conpty", 19041) });
        Assert.True(IsSecondLineWrapped(terminal));
    }

    [Fact]
    public void A_recent_ConPTY_build_does_not_use_the_heuristics()
    {
        using TerminalCore terminal = CreateTerminal(TerminalOptions.Default with { WindowsPty = new WindowsPtyOptions("conpty", 22631) });
        Assert.False(IsSecondLineWrapped(terminal));
    }

    [Fact]
    public void WindowsMode_uses_the_heuristics()
    {
        using TerminalCore terminal = CreateTerminal(TerminalOptions.Default with { WindowsMode = true });
        Assert.True(IsSecondLineWrapped(terminal));
    }

    [Fact]
    public void Without_any_Windows_option_the_heuristics_are_off()
    {
        using TerminalCore terminal = CreateTerminal(TerminalOptions.Default);
        Assert.False(IsSecondLineWrapped(terminal));
    }

    [Fact]
    public void A_line_feed_after_a_space_does_not_mark_the_next_line_as_wrapped()
    {
        using TerminalCore terminal = CreateTerminal(TerminalOptions.Default with { WindowsMode = true });
        terminal.Write(new string('a', 79) + " ");
        terminal.Write("\n");
        Assert.False(terminal.Buffers.Active.Lines.Get(1)!.IsWrapped);
    }

    [Fact]
    public void Changing_the_option_at_run_time_switches_the_heuristics()
    {
        using TerminalCore terminal = CreateTerminal(TerminalOptions.Default with { WindowsPty = new WindowsPtyOptions("conpty", 19041) });
        terminal.OptionsService.SetOptions(terminal.Options with { WindowsPty = new WindowsPtyOptions("conpty", 22631) });
        Assert.False(IsSecondLineWrapped(terminal));

        terminal.OptionsService.SetOptions(terminal.Options with { WindowsPty = new WindowsPtyOptions("conpty", 19041) });
        terminal.Write("\x1b[H");
        Assert.True(IsSecondLineWrapped(terminal));
    }

    [Fact]
    public void CUP_updates_the_wrapped_state_of_the_cursor_line_before_moving_the_cursor()
    {
        using TerminalCore terminal = CreateTerminal(TerminalOptions.Default with { WindowsMode = true });
        terminal.Write(new string('a', 80));
        terminal.Write("\n");
        terminal.Buffers.Active.Lines.Get(1)!.IsWrapped = false;

        // The cursor is on line 1: the heuristics look at it, then CUP moves the cursor.
        terminal.Write("\x1b[H");
        Assert.True(terminal.Buffers.Active.Lines.Get(1)!.IsWrapped);
        Assert.Equal(0, terminal.Buffers.Active.Y);
    }

    private static bool ReflowsWhenResizedNarrower(TerminalOptions options)
    {
        using TerminalCore terminal = CreateTerminal(options);
        // The cursor line is never reflowed (ReflowCursorLine is off), so move the cursor away first.
        terminal.Write(new string('a', 40) + "\r\n");
        terminal.Resize(20, 24);
        return terminal.Buffers.Active.Lines.Get(1)!.IsWrapped;
    }

    // Not an xterm.js test: documents Buffer._isReflowEnabled with a windowsPty build number.
    [Fact]
    public void A_recent_ConPTY_build_reflows_when_resized_narrower()
    {
        Assert.True(ReflowsWhenResizedNarrower(TerminalOptions.Default with { WindowsPty = new WindowsPtyOptions("conpty", 22631) }));
    }

    [Fact]
    public void An_old_ConPTY_build_does_not_reflow_when_resized_narrower()
    {
        Assert.False(ReflowsWhenResizedNarrower(TerminalOptions.Default with { WindowsPty = new WindowsPtyOptions("conpty", 19041) }));
    }
}
