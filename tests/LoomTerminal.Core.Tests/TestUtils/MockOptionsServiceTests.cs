using LoomTerminal.Core.Services;

namespace LoomTerminal.Core.Tests.TestUtils;

public class MockOptionsServiceTests
{
    [Fact]
    public void Assigning_RawOptions_raises_OnOptionChange_for_each_changed_option()
    {
        MockOptionsService options = new();
        List<TerminalOption> fired = [];
        options.OnOptionChange.Subscribe(fired.Add);

        options.RawOptions = options.RawOptions with { ConvertEol = true, Cols = 100, Rows = 24 };

        Assert.Equal([TerminalOption.Cols, TerminalOption.ConvertEol], fired);
        Assert.True(options.Options.ConvertEol);
    }

    [Fact]
    public void MockUnicodeService_uses_the_V6_widths()
    {
        MockUnicodeService unicode = new();
        Assert.Equal(1, unicode.Wcwidth(0x1F600));
        Assert.Equal(2, unicode.Wcwidth(0x4E00));
    }
}
