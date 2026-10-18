namespace LoomTerminal.Core.Tests.InputHandling;

public sealed class InputHandlerTabTests : InputHandlerTestBase
{
    [Fact]
    public void Default_tab_stops_are_every_8_columns()
    {
        InputHandler.Parse("\t");
        Assert.Equal(8, BufferService.Buffer.X);
        InputHandler.Parse("\t");
        Assert.Equal(16, BufferService.Buffer.X);
    }

    [Fact]
    public void ESC_H_sets_a_tab_stop_at_the_cursor()
    {
        InputHandler.Parse("abc\x1bH\r\t");
        Assert.Equal(3, BufferService.Buffer.X);
    }

    [Fact]
    public void C1_HTS_sets_a_tab_stop_at_the_cursor()
    {
        InputHandler.Parse("abc\u0088\r\t");
        Assert.Equal(3, BufferService.Buffer.X);
    }

    [Fact]
    public void TBC_0_clears_the_tab_stop_at_the_cursor()
    {
        InputHandler.Parse("\t\x1b[0g\r\t");
        Assert.Equal(16, BufferService.Buffer.X);
    }

    [Fact]
    public void TBC_3_clears_all_tab_stops()
    {
        InputHandler.Parse("\x1b[3g\t");
        Assert.Equal(79, BufferService.Buffer.X);
    }

    [Fact]
    public void CHT_moves_forward_by_the_given_number_of_tab_stops()
    {
        InputHandler.Parse("\x1b[2I");
        Assert.Equal(16, BufferService.Buffer.X);
    }

    [Fact]
    public void CBT_moves_backward_by_one_tab_stop_by_default()
    {
        InputHandler.Parse("\x1b[2I\x1b[Z");
        Assert.Equal(8, BufferService.Buffer.X);
    }
}
