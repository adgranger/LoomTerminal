using LoomTerminal.Presentation.Input;

namespace LoomTerminal.Presentation.Tests.Input;

public class TerminalKeyEventTests
{
    private static TerminalKeyEvent Sample() =>
        new(DomKeyCodes.KeyA, "a", false, false, false, false, TerminalKeyEventType.KeyDown);

    [Fact]
    public void Records_with_the_same_values_are_equal()
    {
        Assert.Equal(Sample(), Sample());
    }

    [Fact]
    public void With_changes_one_value_and_keeps_the_rest()
    {
        var ctrl = Sample() with { Ctrl = true };

        Assert.True(ctrl.Ctrl);
        Assert.Equal(DomKeyCodes.KeyA, ctrl.KeyCode);
        Assert.NotEqual(Sample(), ctrl);
    }

    [Fact]
    public void AltGraph_defaults_to_false_and_can_be_set()
    {
        Assert.False(Sample().AltGraph);
        Assert.True((Sample() with { AltGraph = true }).AltGraph);
    }

    [Fact]
    public void Dom_key_codes_match_the_DOM_spec()
    {
        Assert.Equal(8, DomKeyCodes.Backspace);
        Assert.Equal(13, DomKeyCodes.Enter);
        Assert.Equal(27, DomKeyCodes.Escape);
        Assert.Equal(37, DomKeyCodes.ArrowLeft);
        Assert.Equal(48, DomKeyCodes.Digit0);
        Assert.Equal(65, DomKeyCodes.KeyA);
        Assert.Equal(90, DomKeyCodes.KeyZ);
        Assert.Equal(112, DomKeyCodes.F1);
        Assert.Equal(123, DomKeyCodes.F12);
        Assert.Equal(186, DomKeyCodes.Semicolon);
        Assert.Equal(222, DomKeyCodes.Quote);
    }
}
