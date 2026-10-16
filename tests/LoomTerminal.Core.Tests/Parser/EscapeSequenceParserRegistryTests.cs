// Ported from xtermjs/xterm.js: src/common/parser/EscapeSequenceParser.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Parser;

namespace LoomTerminal.Core.Tests.Parser;

// New tests: xterm.js tests the registry only through the parse loop (ported by P1-11 to P1-14).
public class EscapeSequenceParserRegistryTests
{
    private sealed class RegistryParser : EscapeSequenceParser
    {
        public int CsiHandlerCount(FunctionIdentifier id) =>
            _csiHandlers.TryGetValue(Identifier(id), out List<CsiHandler>? list) ? list.Count : 0;

        public int EscHandlerCount(FunctionIdentifier id) =>
            _escHandlers.TryGetValue(Identifier(id, 0x30, 0x7e), out List<EscHandler>? list) ? list.Count : 0;

        public static int IdentifierOf(FunctionIdentifier id) => Identifier(id);
    }

    [Fact]
    public void Registering_and_disposing_a_CSI_handler_leaves_no_handler()
    {
        using RegistryParser parser = new();
        FunctionIdentifier id = new(Final: "m");

        IDisposable first = parser.RegisterCsiHandler(id, _ => true);
        IDisposable second = parser.RegisterCsiHandler(id, _ => true);
        Assert.Equal(2, parser.CsiHandlerCount(id));

        first.Dispose();
        first.Dispose();
        Assert.Equal(1, parser.CsiHandlerCount(id));

        second.Dispose();
        Assert.Equal(0, parser.CsiHandlerCount(id));
    }

    [Fact]
    public void ClearCsiHandler_removes_every_handler_of_the_identifier()
    {
        using RegistryParser parser = new();
        FunctionIdentifier id = new(Final: "m");
        parser.RegisterCsiHandler(id, _ => true);
        parser.RegisterCsiHandler(id, _ => true);

        parser.ClearCsiHandler(id);

        Assert.Equal(0, parser.CsiHandlerCount(id));
    }

    [Fact]
    public void The_parser_swallows_the_7bit_ST_by_default()
    {
        using RegistryParser parser = new();
        Assert.Equal(1, parser.EscHandlerCount(new FunctionIdentifier(Final: "\\")));
    }

    [Fact]
    public void IdentToString_returns_the_prefix_intermediates_and_final()
    {
        using RegistryParser parser = new();
        int ident = RegistryParser.IdentifierOf(new FunctionIdentifier(Prefix: "?", Intermediates: "$", Final: "p"));
        Assert.Equal("?$p", parser.IdentToString(ident));
    }

    [Theory]
    [InlineData(null, null, "ab")]
    [InlineData(null, null, "")]
    [InlineData(null, null, " ")]
    [InlineData(";", null, "p")]
    [InlineData("??", null, "p")]
    [InlineData(null, "0", "p")]
    [InlineData(null, "$$$", "p")]
    public void Invalid_identifiers_throw(string? prefix, string? intermediates, string final)
    {
        FunctionIdentifier id = new(prefix, intermediates, final);
        Assert.Throws<ArgumentException>(() => RegistryParser.IdentifierOf(id));
    }

    [Fact]
    public void Disposing_the_parser_clears_the_handlers()
    {
        RegistryParser parser = new();
        FunctionIdentifier id = new(Final: "m");
        parser.RegisterCsiHandler(id, _ => true);

        parser.Dispose();

        Assert.Equal(0, parser.CsiHandlerCount(id));
        Assert.True(parser.IsDisposed);
    }
}
