using System.Text;
using LoomTerminal.Core.Input;
using LoomTerminal.Core.Parser;
using static LoomTerminal.Core.Tests.Parser.ParserTestHelpers;

namespace LoomTerminal.Core.Tests.Parser;

// New test: the xterm.js parser tests are ported by P1-11 to P1-14.
public class EscapeSequenceParserParseSmokeTests
{
    [Fact]
    public void Parse_dispatches_a_CSI_sequence_and_prints_the_text()
    {
        using EscapeSequenceParser parser = new();
        StringBuilder printed = new();
        List<int[]> csiParams = [];
        parser.SetPrintHandler((data, start, end) => printed.Append(TextDecoder.Utf32ToString(data, start, end)));
        parser.RegisterCsiHandler(new FunctionIdentifier(Final: "m"), p =>
        {
            csiParams.Add(p.ParamValues[..p.Length]);
            return true;
        });

        uint[] data = ToUtf32("\u001b[1;31mHello");
        parser.Parse(data, data.Length);

        Assert.Equal("Hello", printed.ToString());
        int[] only = Assert.Single(csiParams);
        Assert.Equal([1, 31], only);
    }
}
