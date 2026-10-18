using LoomTerminal.Core.Data;
using LoomTerminal.Core.Services;

namespace LoomTerminal.Core.Tests.Data;

// The xterm.js sources of the charsets and of CharsetService have no unit tests.
public class CharsetsTests
{
    [Fact]
    public void DEC_special_graphics_maps_line_drawing_characters()
    {
        IReadOnlyDictionary<char, string> table = Charsets.All['0']!;
        Assert.Equal("─", table['q']);
        Assert.Equal("│", table['x']);
        Assert.Equal(31, table.Count);
    }

    [Fact]
    public void US_ASCII_has_no_table_and_is_the_default()
    {
        Assert.True(Charsets.All.ContainsKey('B'));
        Assert.Null(Charsets.All['B']);
        Assert.Null(Charsets.DefaultCharset);
    }

    [Fact]
    public void National_charsets_with_two_designators_share_one_table()
    {
        Assert.Same(Charsets.All['C'], Charsets.All['5']);
        Assert.Same(Charsets.All['E'], Charsets.All['6']);
        Assert.Same(Charsets.All['H'], Charsets.All['7']);
        Assert.Equal("£", Charsets.All['A']!['#']);
        Assert.Equal("ç", Charsets.All['R']!['\\']);
    }

    [Fact]
    public void SetgCharset_then_SetgLevel_activates_the_table_and_Reset_restores_the_default()
    {
        CharsetService service = new();
        service.SetgCharset(0, Charsets.All['0']);
        service.SetgLevel(0);
        Assert.Same(Charsets.All['0'], service.Charset);

        service.SetgCharset(1, Charsets.All['A']);
        Assert.Same(Charsets.All['0'], service.Charset);
        service.SetgLevel(1);
        Assert.Same(Charsets.All['A'], service.Charset);
        service.SetgCharset(1, null);
        Assert.Null(service.Charset);

        service.SetgCharset(0, Charsets.All['0']);
        service.Reset();
        Assert.Null(service.Charset);
        Assert.Equal(0, service.GLevel);
        Assert.All(service.Charsets, Assert.Null);
    }
}
