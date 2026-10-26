using LoomTerminal.Presentation.Input;

namespace LoomTerminal.Presentation.Tests.Input;

/// <summary>The macOS rules of <see cref="KeyInputHandler"/>: Cmd+C, Cmd+V and Cmd+K; Ctrl keeps its terminal meaning.</summary>
public class KeyInputHandlerMacTests
{
    private static readonly KeyContext Mac = new(ApplicationCursorKeys: false, HasSelection: false, IsMac: true);
    private static readonly KeyContext MacWithSelection = Mac with { HasSelection = true };

    private static TerminalKeyEvent Down(int keyCode, string key, bool ctrl = false, bool alt = false, bool shift = false, bool meta = false) =>
        new(keyCode, key, ctrl, alt, shift, meta, TerminalKeyEventType.KeyDown);

    [Fact]
    public void Cmd_C_copies_the_selection()
    {
        var result = new KeyInputHandler().Handle(Down(DomKeyCodes.KeyC, "c", meta: true), MacWithSelection);

        Assert.Equal(KeyInputOutcome.Copy, result.Outcome);
    }

    [Fact]
    public void Cmd_C_without_a_selection_does_nothing()
    {
        var result = new KeyInputHandler().Handle(Down(DomKeyCodes.KeyC, "c", meta: true), Mac);

        Assert.Equal(KeyInputOutcome.Ignored, result.Outcome);
    }

    [Fact]
    public void Cmd_V_pastes()
    {
        var result = new KeyInputHandler().Handle(Down(DomKeyCodes.KeyV, "v", meta: true), Mac);

        Assert.Equal(KeyInputOutcome.Paste, result.Outcome);
    }

    [Fact]
    public void Cmd_K_clears_the_scrollback()
    {
        var result = new KeyInputHandler().Handle(Down(DomKeyCodes.KeyK, "k", meta: true), Mac);

        Assert.Equal(KeyInputOutcome.ClearScrollback, result.Outcome);
    }

    [Fact]
    public void Ctrl_C_is_the_interrupt_even_with_a_selection()
    {
        var result = new KeyInputHandler().Handle(Down(DomKeyCodes.KeyC, "c", ctrl: true), MacWithSelection);

        Assert.Equal(KeyInputOutcome.Sent, result.Outcome);
        Assert.Equal("\u0003", result.Data);
    }

    [Fact]
    public void Ctrl_V_is_not_a_paste_on_macOS()
    {
        var result = new KeyInputHandler().Handle(Down(DomKeyCodes.KeyV, "v", ctrl: true), Mac);

        Assert.Equal(KeyInputOutcome.Sent, result.Outcome);
        Assert.Equal("\u0016", result.Data);
    }

    [Fact]
    public void Cmd_C_is_not_a_copy_off_macOS()
    {
        var result = new KeyInputHandler().Handle(Down(DomKeyCodes.KeyC, "c", meta: true), new KeyContext(false, true));

        Assert.NotEqual(KeyInputOutcome.Copy, result.Outcome);
    }

    [Fact]
    public void Option_letter_types_the_accented_character_unless_Option_is_Meta()
    {
        // Option+E on a US layout: nothing is sent and the key stays unhandled, so the text input types the accent.
        var typed = new KeyInputHandler().Handle(Down(DomKeyCodes.KeyE, "´", alt: true), Mac);
        var meta = new KeyInputHandler().Handle(Down(DomKeyCodes.KeyE, "e", alt: true), Mac with { MacOptionIsMeta = true });

        Assert.Equal(KeyInputOutcome.Ignored, typed.Outcome);
        Assert.Equal(KeyInputOutcome.Sent, meta.Outcome);
        Assert.Equal("\u001be", meta.Data);
    }
}
