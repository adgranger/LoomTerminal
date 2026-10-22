using LoomTerminal.Presentation.Input;

namespace LoomTerminal.Presentation.Tests.Input;

public class KeyInputHandlerTests
{
    private static readonly KeyContext Plain = new(ApplicationCursorKeys: false, HasSelection: false);

    private static TerminalKeyEvent Down(
        int keyCode,
        string key = "",
        bool ctrl = false,
        bool alt = false,
        bool shift = false,
        bool altGraph = false) =>
        new(keyCode, key, ctrl, alt, shift, false, TerminalKeyEventType.KeyDown) { AltGraph = altGraph };

    [Fact]
    public void Key_preview_returning_true_hands_the_key_to_the_host()
    {
        var handler = new KeyInputHandler { KeyPreview = _ => true };

        var result = handler.Handle(Down(DomKeyCodes.KeyT, "T", ctrl: true, shift: true), Plain);

        Assert.Equal(KeyInputOutcome.HandledByHost, result.Outcome);
    }

    [Fact]
    public void Key_preview_is_not_offered_AltGr_combinations()
    {
        var offered = false;
        var handler = new KeyInputHandler { KeyPreview = _ => offered = true };

        handler.Handle(Down(DomKeyCodes.KeyQ, "@", ctrl: true, alt: true, altGraph: true), Plain);

        Assert.False(offered);
    }

    [Fact]
    public void Ctrl_alt_Q_with_AltGraph_is_left_to_the_text_input()
    {
        var result = new KeyInputHandler().Handle(Down(DomKeyCodes.KeyQ, "@", ctrl: true, alt: true, altGraph: true), Plain);

        Assert.Equal(KeyInputOutcome.LeaveToTextInput, result.Outcome);
    }

    [Fact]
    public void Ctrl_shift_C_copies_even_without_selection()
    {
        var result = new KeyInputHandler().Handle(Down(DomKeyCodes.KeyC, "C", ctrl: true, shift: true), Plain);

        Assert.Equal(KeyInputOutcome.Copy, result.Outcome);
    }

    [Fact]
    public void Ctrl_C_with_a_selection_copies()
    {
        var result = new KeyInputHandler().Handle(Down(DomKeyCodes.KeyC, "c", ctrl: true), Plain with { HasSelection = true });

        Assert.Equal(KeyInputOutcome.Copy, result.Outcome);
    }

    [Fact]
    public void Ctrl_C_without_a_selection_is_sent_as_etx()
    {
        var result = new KeyInputHandler().Handle(Down(DomKeyCodes.KeyC, "c", ctrl: true), Plain);

        Assert.Equal(KeyInputOutcome.Sent, result.Outcome);
        Assert.Equal("\u0003", result.Data);
    }

    [Fact]
    public void Ctrl_V_ctrl_shift_V_and_shift_insert_paste()
    {
        var handler = new KeyInputHandler();

        Assert.Equal(KeyInputOutcome.Paste, handler.Handle(Down(DomKeyCodes.KeyV, "v", ctrl: true), Plain).Outcome);
        Assert.Equal(KeyInputOutcome.Paste, handler.Handle(Down(DomKeyCodes.KeyV, "V", ctrl: true, shift: true), Plain).Outcome);
        Assert.Equal(KeyInputOutcome.Paste, handler.Handle(Down(DomKeyCodes.Insert, "Insert", shift: true), Plain).Outcome);
    }

    [Fact]
    public void Arrow_up_in_application_cursor_mode_sends_ss3_A()
    {
        var result = new KeyInputHandler().Handle(
            Down(DomKeyCodes.ArrowUp, "ArrowUp"),
            Plain with { ApplicationCursorKeys = true });

        Assert.Equal(KeyInputOutcome.Sent, result.Outcome);
        Assert.Equal("\u001bOA", result.Data);
    }

    [Fact]
    public void Space_has_no_sequence_and_is_left_to_the_text_input()
    {
        var result = new KeyInputHandler().Handle(Down(DomKeyCodes.Space, " "), Plain);

        Assert.Equal(KeyInputOutcome.LeaveToTextInput, result.Outcome);
    }

    [Fact]
    public void Key_press_events_are_ignored()
    {
        var press = Down(DomKeyCodes.KeyA, "a") with { Type = TerminalKeyEventType.KeyPress };

        Assert.Equal(KeyInputOutcome.Ignored, new KeyInputHandler().Handle(press, Plain).Outcome);
    }
}
