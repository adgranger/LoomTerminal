namespace LoomTerminal.Core.Tests.InputHandling;

public sealed class InputHandlerDeviceAttributeTests : InputHandlerTestBase
{
    private readonly List<string> _sent = [];

    public InputHandlerDeviceAttributeTests() => CoreService.OnData.Subscribe(_sent.Add);

    [Fact]
    public void REP_repeats_the_preceding_character()
    {
        InputHandler.Parse("a\x1b[3b");
        Assert.Equal<string>(["aaaa"], GetLines(1));
    }

    [Fact]
    public void REP_without_a_preceding_character_does_nothing()
    {
        InputHandler.Parse("\x1b[3b");
        Assert.Equal<string>([""], GetLines(1));
    }

    [Fact]
    public void REP_repeats_a_wide_character()
    {
        InputHandler.Parse("￥\x1b[2b");
        Assert.Equal<string>(["￥￥￥"], GetLines(1));
    }

    [Fact]
    public void DA1_of_xterm_sends_the_primary_attributes()
    {
        InputHandler.Parse("\x1b[c");
        Assert.Equal<string>(["\x1b[?1;2c"], _sent);
    }

    [Fact]
    public void DA1_with_a_parameter_sends_nothing()
    {
        InputHandler.Parse("\x1b[1c");
        Assert.Empty(_sent);
    }

    [Fact]
    public void DA1_of_linux_sends_its_own_attributes()
    {
        OptionsService.RawOptions = OptionsService.RawOptions with { TermName = "linux" };
        InputHandler.Parse("\x1b[c");
        Assert.Equal<string>(["\x1b[?6c"], _sent);
    }

    [Fact]
    public void DA2_of_xterm_sends_the_secondary_attributes()
    {
        InputHandler.Parse("\x1b[>c");
        Assert.Equal<string>(["\x1b[>0;276;0c"], _sent);
    }

    [Fact]
    public void DA2_of_screen_and_rxvt_unicode_send_their_own_attributes()
    {
        OptionsService.RawOptions = OptionsService.RawOptions with { TermName = "screen-256color" };
        InputHandler.Parse("\x1b[>c");
        OptionsService.RawOptions = OptionsService.RawOptions with { TermName = "rxvt-unicode" };
        InputHandler.Parse("\x1b[>c");
        Assert.Equal<string>(["\x1b[>83;40003;0c", "\x1b[>85;95;0c"], _sent);
    }
}
