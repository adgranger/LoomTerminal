using LoomTerminal.Presentation.Rendering;
using Microsoft.Extensions.Time.Testing;

namespace LoomTerminal.Presentation.Tests.Rendering;

public class CursorBlinkStateTests
{
    private readonly FakeTimeProvider _time = new();

    private bool Visible(CursorBlinkState state) => state.IsVisible(_time.GetTimestamp());

    [Fact]
    public void The_cursor_is_visible_for_600_ms_then_hidden_then_visible_again()
    {
        var state = new CursorBlinkState(_time);
        Assert.True(Visible(state));

        _time.Advance(TimeSpan.FromMilliseconds(599));
        Assert.True(Visible(state));

        _time.Advance(TimeSpan.FromMilliseconds(1));
        Assert.False(Visible(state));

        _time.Advance(TimeSpan.FromMilliseconds(600));
        Assert.True(Visible(state));
    }

    [Fact]
    public void Restart_makes_the_cursor_visible_immediately_and_restarts_the_cycle()
    {
        var state = new CursorBlinkState(_time);
        _time.Advance(TimeSpan.FromMilliseconds(700));
        Assert.False(Visible(state));

        state.Restart();

        Assert.True(Visible(state));
        _time.Advance(TimeSpan.FromMilliseconds(599));
        Assert.True(Visible(state));
        _time.Advance(TimeSpan.FromMilliseconds(1));
        Assert.False(Visible(state));
    }

    [Fact]
    public void Pause_stops_the_blinking_and_ignores_restart_until_resumed()
    {
        var state = new CursorBlinkState(_time);
        _time.Advance(TimeSpan.FromMilliseconds(700));

        state.Pause();
        _time.Advance(TimeSpan.FromSeconds(10));
        state.Restart();

        Assert.True(Visible(state));
        Assert.Null(state.NextChange);

        state.Resume();
        Assert.True(Visible(state));
        _time.Advance(TimeSpan.FromMilliseconds(600));
        Assert.False(Visible(state));
    }

    [Fact]
    public void NextChange_is_the_time_left_in_the_current_phase()
    {
        var state = new CursorBlinkState(_time);
        Assert.Equal(TimeSpan.FromMilliseconds(600), state.NextChange);

        _time.Advance(TimeSpan.FromMilliseconds(250));
        Assert.Equal(TimeSpan.FromMilliseconds(350), state.NextChange);

        _time.Advance(TimeSpan.FromMilliseconds(450));
        Assert.Equal(TimeSpan.FromMilliseconds(500), state.NextChange);
    }
}
