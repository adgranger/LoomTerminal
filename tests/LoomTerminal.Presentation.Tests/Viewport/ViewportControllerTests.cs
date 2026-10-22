using LoomTerminal.Core;
using LoomTerminal.Core.Services;
using LoomTerminal.Presentation.Input;
using LoomTerminal.Presentation.Session;
using LoomTerminal.Presentation.Tests.Session;
using LoomTerminal.Presentation.Viewport;
using PointerWheelMode = LoomTerminal.Presentation.Input.WheelDeltaMode;

namespace LoomTerminal.Presentation.Tests.Viewport;

public sealed class ViewportControllerTests : IDisposable
{
    private readonly TerminalCore _core = new(new TerminalOptions { Cols = 20, Rows = 5 });
    private readonly ViewportController _controller;
    private readonly List<string> _sent = [];

    public ViewportControllerTests()
    {
        _controller = new ViewportController(_core);
        _core.OnData.Subscribe(_sent.Add);
    }

    public void Dispose()
    {
        _controller.Dispose();
        _core.Dispose();
    }

    private static TerminalPointerEvent Wheel(double deltaY, bool shift = false) =>
        new(0, 0, 0, 0, deltaY < 0 ? PointerButton.WheelUp : PointerButton.WheelDown, PointerAction.Wheel, false, false, shift)
        {
            WheelDeltaY = deltaY,
            DeltaMode = PointerWheelMode.Line,
        };

    private void FillScrollback() => _core.Write(string.Join("\r\n", Enumerable.Range(0, 30)));

    [Fact]
    public void The_wheel_is_reported_to_an_application_that_tracks_the_mouse()
    {
        _core.Write("\u001b[?1000h\u001b[?1006h");

        _controller.HandleWheel(Wheel(-1), 20);
        _controller.HandleWheel(Wheel(1), 20);

        Assert.Equal(["\u001b[<64;1;1M", "\u001b[<65;1;1M"], _sent);
    }

    [Fact]
    public void Shift_keeps_the_wheel_from_being_reported_to_the_application()
    {
        _core.Write("\u001b[?1000h\u001b[?1006h");

        _controller.HandleWheel(Wheel(-1, shift: true), 20);

        Assert.Empty(_sent);
    }

    [Fact]
    public void Without_scrollback_the_wheel_sends_a_single_arrow_sequence()
    {
        _core.Write("\u001b[?1049h");

        _controller.HandleWheel(Wheel(-3), 20);
        _controller.HandleWheel(Wheel(3), 20);

        Assert.Equal(["\u001b[A", "\u001b[B"], _sent);
    }

    [Fact]
    public void Without_scrollback_the_arrow_sequence_follows_the_application_cursor_keys()
    {
        _core.Write("\u001b[?1049h\u001b[?1h");

        _controller.HandleWheel(Wheel(-1), 20);
        _controller.HandleWheel(Wheel(1), 20);

        Assert.Equal(["\u001bOA", "\u001bOB"], _sent);
    }

    [Fact]
    public void With_scrollback_the_wheel_scrolls_the_viewport()
    {
        FillScrollback();
        var bottom = _core.Buffers.Active.YDisp;
        Assert.True(bottom > 3);

        _controller.HandleWheel(Wheel(-3), 20);
        Assert.Equal(bottom - 3, _core.Buffers.Active.YDisp);

        _controller.HandleWheel(Wheel(2), 20);
        Assert.Equal(bottom - 1, _core.Buffers.Active.YDisp);
        Assert.Empty(_sent);
    }

    [Fact]
    public void A_wheel_event_without_vertical_delta_does_nothing()
    {
        FillScrollback();
        var bottom = _core.Buffers.Active.YDisp;

        _controller.HandleWheel(Wheel(0), 20);

        Assert.Equal(bottom, _core.Buffers.Active.YDisp);
    }

    // Session level: the public scroll methods and the follow-the-output rules.
    private static async Task<(int YDisp, int YBase)> PositionAsync(TerminalSession session) =>
        await session.RunOnParserThreadAsync(core => (core.Buffers.Active.YDisp, core.Buffers.Active.YBase));

    private static async Task<TerminalSession> SessionWithScrollbackAsync(FakePtyConnection fake)
    {
        var session = TerminalSession.Attach(fake, 20, 5);
        fake.Emit(string.Join("\r\n", Enumerable.Range(0, 30)));
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while ((await PositionAsync(session)).YBase == 0)
        {
            await Task.Delay(5, cts.Token);
        }

        return session;
    }

    [Fact]
    public async Task ScrollPages_moves_by_rows_minus_one()
    {
        var fake = new FakePtyConnection();
        using var session = await SessionWithScrollbackAsync(fake);
        var (_, yBase) = await PositionAsync(session);

        session.ScrollPages(-1);

        Assert.Equal(yBase - 4, (await PositionAsync(session)).YDisp);
    }

    [Fact]
    public async Task ScrollToTop_and_ScrollToBottom_reach_the_ends()
    {
        var fake = new FakePtyConnection();
        using var session = await SessionWithScrollbackAsync(fake);
        var (_, yBase) = await PositionAsync(session);

        session.ScrollToTop();
        Assert.Equal(0, (await PositionAsync(session)).YDisp);

        session.ScrollToBottom();
        Assert.Equal(yBase, (await PositionAsync(session)).YDisp);
    }

    [Fact]
    public async Task Typing_scrolls_to_the_bottom()
    {
        var fake = new FakePtyConnection();
        using var session = await SessionWithScrollbackAsync(fake);
        var (_, yBase) = await PositionAsync(session);
        session.ScrollToTop();

        session.SendText("a");

        Assert.Equal(yBase, (await PositionAsync(session)).YDisp);
    }

    [Fact]
    public async Task Output_while_scrolled_up_keeps_the_viewport_in_place()
    {
        var fake = new FakePtyConnection();
        using var session = await SessionWithScrollbackAsync(fake);
        session.ScrollLines(-5);
        var (before, yBaseBefore) = await PositionAsync(session);

        fake.Emit("\r\nmore\r\noutput");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while ((await PositionAsync(session)).YBase == yBaseBefore)
        {
            await Task.Delay(5, cts.Token);
        }

        Assert.Equal(before, (await PositionAsync(session)).YDisp);
    }

    [Fact]
    public async Task HandleWheel_on_the_session_scrolls_the_viewport()
    {
        var fake = new FakePtyConnection();
        using var session = await SessionWithScrollbackAsync(fake);
        var (yDisp, _) = await PositionAsync(session);

        session.HandleWheel(Wheel(-2), 20);

        Assert.Equal(yDisp - 2, (await PositionAsync(session)).YDisp);
    }
}
