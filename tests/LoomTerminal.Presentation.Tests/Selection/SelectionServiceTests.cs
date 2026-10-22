// Ported from xtermjs/xterm.js: src/browser/services/SelectionService.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core;
using LoomTerminal.Core.Buffer;
using LoomTerminal.Core.Services;
using LoomTerminal.Presentation.Selection;

namespace LoomTerminal.Presentation.Tests.Selection;

public sealed class SelectionServiceTests : IDisposable
{
    private readonly TerminalCore _core = new(new TerminalOptions { Cols = 20, Rows = 20 });
    private readonly SelectionService _service;

    public SelectionServiceTests()
    {
        _service = new SelectionService(_core.BufferService, _core.CoreService, _core.OptionsService, isWindows: false);
    }

    private Core.Buffer.Buffer Buffer => _core.Buffers.Active;

    public void Dispose()
    {
        _service.Dispose();
        _core.Dispose();
    }

    private static BufferLine StringToRow(string text)
    {
        var result = new BufferLine(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            result.SetCell(i, CellData.FromCharData(new CharData(0, text[i].ToString(), 1, text[i])));
        }

        return result;
    }

    private static BufferLine StringArrayToRow(string[] chars)
    {
        var line = new BufferLine(chars.Length);
        for (var i = 0; i < chars.Length; i++)
        {
            line.SetCell(i, CellData.FromCharData(new CharData(0, chars[i], 1, chars[i][0])));
        }

        return line;
    }

    private static BufferLine RowOf(CharData[] data)
    {
        var line = new BufferLine(data.Length);
        for (var i = 0; i < data.Length; i++)
        {
            line.SetCell(i, CellData.FromCharData(data[i]));
        }

        return line;
    }

    // Selects the word at each column of row 0 and checks the selected text.
    private void AssertWordAtEachColumn(params string[] expected)
    {
        for (var x = 0; x < expected.Length; x++)
        {
            _service.SelectWordAt((x, 0), true);
            Assert.Equal(expected[x], _service.SelectionText);
        }
    }

    // _selectWordAt: should expand selection for normal width chars
    [Fact]
    public void SelectWordAt_expands_for_normal_width_chars()
    {
        Buffer.Lines.Set(0, StringToRow("foo bar"));

        AssertWordAtEachColumn("foo", "foo", "foo", " ", "bar", "bar", "bar");
    }

    // _selectWordAt: should expand selection for whitespace
    [Fact]
    public void SelectWordAt_expands_for_whitespace()
    {
        Buffer.Lines.Set(0, StringToRow("a   b"));

        AssertWordAtEachColumn("a", "   ", "   ", "   ", "b");
    }

    // _selectWordAt: should expand selection for wide characters
    [Fact]
    public void SelectWordAt_expands_for_wide_characters()
    {
        const string Zhong = "中"; // 中
        const string Wen = "文"; // 文

        // Wide characters use a special format
        Buffer.Lines.Set(0, RowOf(
        [
            new CharData(0, Zhong, 2, Zhong[0]),
            new CharData(0, string.Empty, 0, 0),
            new CharData(0, Wen, 2, Wen[0]),
            new CharData(0, string.Empty, 0, 0),
            new CharData(0, " ", 1, ' '),
            new CharData(0, "a", 1, 'a'),
            new CharData(0, Zhong, 2, Zhong[0]),
            new CharData(0, string.Empty, 0, 0),
            new CharData(0, Wen, 2, Wen[0]),
            new CharData(0, string.Empty, 0, 0),
            new CharData(0, "b", 1, 'b'),
            new CharData(0, " ", 1, ' '),
            new CharData(0, "f", 1, 'f'),
            new CharData(0, "o", 1, 'o'),
            new CharData(0, "o", 1, 'o'),
        ]));

        var wide = Zhong + Wen;
        var wrapped = "a" + Zhong + Wen + "b";
        AssertWordAtEachColumn(wide, wide, wide, wide, " ", wrapped, wrapped, wrapped, wrapped, wrapped, wrapped, " ", "foo", "foo", "foo");
    }

    // _selectWordAt: should select up to non-path characters that are commonly adjacent to paths
    [Fact]
    public void SelectWordAt_stops_at_characters_commonly_adjacent_to_paths()
    {
        Buffer.Lines.Set(0, StringToRow("(cd)[ef]{gh}'ij\""));

        AssertWordAtEachColumn(
            "(cd", "cd", "cd", "cd)", "[ef", "ef", "ef", "ef]", "{gh", "gh", "gh", "gh}", "'ij", "ij", "ij", "ij\"");
    }

    // _selectWordAt: should expand upwards or downards for wrapped lines
    [Fact]
    public void SelectWordAt_expands_upwards_or_downwards_for_wrapped_lines()
    {
        Buffer.Lines.Set(0, StringToRow("                 foo"));
        Buffer.Lines.Set(1, StringToRow("bar                 "));
        Buffer.Lines.Get(1)!.IsWrapped = true;

        _service.SelectWordAt((1, 1), true);
        Assert.Equal("foobar", _service.SelectionText);

        _service.Model.ClearSelection();
        _service.SelectWordAt((18, 0), true);
        Assert.Equal("foobar", _service.SelectionText);
    }

    // _selectWordAt: should expand both upwards and downwards for word wrapped over many lines
    [Fact]
    public void SelectWordAt_expands_both_ways_for_a_word_wrapped_over_many_lines()
    {
        const string Expected = "fooaaaaaaaaaaaaaaaaaaaabbbbbbbbbbbbbbbbbbbbccccccccccccccccccccbar";
        Buffer.Lines.Set(0, StringToRow("                 foo"));
        Buffer.Lines.Set(1, StringToRow("aaaaaaaaaaaaaaaaaaaa"));
        Buffer.Lines.Set(2, StringToRow("bbbbbbbbbbbbbbbbbbbb"));
        Buffer.Lines.Set(3, StringToRow("cccccccccccccccccccc"));
        Buffer.Lines.Set(4, StringToRow("bar                 "));
        for (var y = 1; y <= 4; y++)
        {
            Buffer.Lines.Get(y)!.IsWrapped = true;
        }

        foreach (var coords in new (int X, int Y)[] { (18, 0), (10, 1), (10, 2), (10, 3), (1, 4) })
        {
            _service.SelectWordAt(coords, true);
            Assert.Equal(Expected, _service.SelectionText);
            _service.Model.ClearSelection();
        }
    }

    // emoji: should treat a single emoji as a word when wrapped in spaces
    [Fact]
    public void A_single_emoji_is_a_word_when_wrapped_in_spaces()
    {
        // The a is here to prevent the space being trimmed in selectionText
        Buffer.Lines.Set(0, StringToRow(" ⚽ a"));

        AssertWordAtEachColumn(" ", "⚽", " ");
    }

    // emoji: should treat multiple emojis as a word when wrapped in spaces
    [Fact]
    public void Multiple_emojis_are_a_word_when_wrapped_in_spaces()
    {
        Buffer.Lines.Set(0, StringToRow(" ⚽⚽ a"));

        AssertWordAtEachColumn(" ", "⚽⚽", "⚽⚽", " ");
    }

    // emoji: should treat emojis using the zero-width-joiner as a single word
    [Fact]
    public void Emojis_using_the_zero_width_joiner_are_a_single_word()
    {
        // Note that the first 3 emojis include the invisible ZWJ char
        const string Family = "\U0001F468‍\U0001F469‍\U0001F467‍\U0001F466";
        Buffer.Lines.Set(0, StringArrayToRow([" ", "\U0001F468‍", "\U0001F469‍", "\U0001F467‍", "\U0001F466", " ", "a"]));

        // ZWJ emojis do not combine in the terminal so the family emoji used here consumed 4 cells. The selection text
        // should retain ZWJ chars despite not combining on the terminal
        AssertWordAtEachColumn(" ", Family, Family, Family, Family, " ");
    }

    // emoji: should treat emojis and characters joined together as a word
    [Fact]
    public void Emojis_and_characters_joined_together_are_a_word()
    {
        Buffer.Lines.Set(0, StringToRow(" ⚽ab cd⚽ ef⚽gh"));

        AssertWordAtEachColumn(
            " ", "⚽ab", "⚽ab", "⚽ab", " ", "cd⚽", "cd⚽", "cd⚽", " ",
            "ef⚽gh", "ef⚽gh", "ef⚽gh", "ef⚽gh", "ef⚽gh");
    }

    // emoji: should treat complex emojis and characters joined together as a word
    [Fact]
    public void Complex_emojis_and_characters_joined_together_are_a_word()
    {
        // The flag for England, made up of: 1F3F4 E0067 E0062 E0065 E006E E0067 E007F
        const string Flag = "\U0001F3F4\U000E0067\U000E0062\U000E0065\U000E006E\U000E0067\U000E007F";
        Buffer.Lines.Set(0, StringArrayToRow([" ", Flag, "a", "b", " ", "c", "d", Flag, " ", "e", "f", Flag, "g", "h", " ", "a"]));

        AssertWordAtEachColumn(
            " ", Flag + "ab", Flag + "ab", Flag + "ab", " ", "cd" + Flag, "cd" + Flag, "cd" + Flag, " ",
            "ef" + Flag + "gh", "ef" + Flag + "gh", "ef" + Flag + "gh", "ef" + Flag + "gh", "ef" + Flag + "gh");
    }

    // _selectLineAt: should select the entire line
    [Fact]
    public void SelectLineAt_selects_the_entire_line()
    {
        Buffer.Lines.Set(0, StringToRow("foo bar"));

        _service.SelectLineAt(0);

        Assert.Equal("foo bar", _service.SelectionText);
        Assert.Equal((0, 0), _service.Model.SelectionStart);
        Assert.Null(_service.Model.SelectionEnd);
        Assert.Equal(20, _service.Model.SelectionStartLength);
        Assert.Equal((0, 0), _service.Model.FinalSelectionStart);
        Assert.Equal((_core.Cols, 0), _service.Model.FinalSelectionEnd);
    }

    // _selectLineAt: should select the entire wrapped line
    [Fact]
    public void SelectLineAt_selects_the_entire_wrapped_line()
    {
        Buffer.Lines.Set(0, StringToRow("foo"));
        var line2 = StringToRow("bar");
        line2.IsWrapped = true;
        Buffer.Lines.Set(1, line2);

        _service.SelectLineAt(0);

        Assert.Equal("foobar", _service.SelectionText);
        Assert.Equal((0, 0), _service.Model.SelectionStart);
        Assert.Null(_service.Model.SelectionEnd);
        Assert.Equal(40, _service.Model.SelectionStartLength);
        Assert.Equal((0, 0), _service.Model.FinalSelectionStart);
        Assert.Equal((_core.Cols, 1), _service.Model.FinalSelectionEnd);
    }

    // selectAll: should select the entire buffer, beyond the viewport
    [Fact]
    public void SelectAll_selects_the_entire_buffer_beyond_the_viewport()
    {
        _core.Resize(20, 5);
        for (var i = 0; i < 5; i++)
        {
            Buffer.Lines.Set(i, StringToRow((i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        _service.SelectAll();

        Assert.Equal("1\n2\n3\n4\n5", _service.SelectionText);
    }

    private void SetLines(params string[] rows)
    {
        Buffer.Lines.Length = rows.Length;
        for (var i = 0; i < rows.Length; i++)
        {
            Buffer.Lines.Set(i, StringToRow(rows[i]));
        }
    }

    // selectLines: should select a single line
    [Fact]
    public void SelectLines_selects_a_single_line()
    {
        SetLines("1", "2", "3");

        _service.SelectLines(1, 1);

        Assert.Equal((0, 1), _service.Model.FinalSelectionStart);
        Assert.Equal((_core.Cols, 1), _service.Model.FinalSelectionEnd);
    }

    // selectLines: should select multiple lines
    [Fact]
    public void SelectLines_selects_multiple_lines()
    {
        SetLines("1", "2", "3", "4", "5");

        _service.SelectLines(1, 3);

        Assert.Equal((0, 1), _service.Model.FinalSelectionStart);
        Assert.Equal((_core.Cols, 3), _service.Model.FinalSelectionEnd);
    }

    // selectLines: should select the to the start when requesting a negative row
    [Fact]
    public void SelectLines_selects_to_the_start_when_requesting_a_negative_row()
    {
        SetLines("1", "2");

        _service.SelectLines(-1, 0);

        Assert.Equal((0, 0), _service.Model.FinalSelectionStart);
        Assert.Equal((_core.Cols, 0), _service.Model.FinalSelectionEnd);
    }

    // selectLines: should select the to the end when requesting beyond the final row
    [Fact]
    public void SelectLines_selects_to_the_end_when_requesting_beyond_the_final_row()
    {
        SetLines("1", "2");

        _service.SelectLines(1, 2);

        Assert.Equal((0, 1), _service.Model.FinalSelectionStart);
        Assert.Equal((_core.Cols, 1), _service.Model.FinalSelectionEnd);
    }

    // hasSelection: should return whether there is a selection
    [Fact]
    public void HasSelection_reports_whether_there_is_a_selection()
    {
        _service.Model.SelectionStart = (0, 0);
        _service.Model.SelectionStartLength = 0;
        Assert.False(_service.HasSelection);

        _service.Model.SelectionEnd = (0, 0);
        Assert.False(_service.HasSelection);

        _service.Model.SelectionEnd = (1, 0);
        Assert.True(_service.HasSelection);

        _service.Model.SelectionEnd = (0, 1);
        Assert.True(_service.HasSelection);

        _service.Model.SelectionEnd = (1, 1);
        Assert.True(_service.HasSelection);
    }

    private void SelectColumns((int X, int Y) start, (int X, int Y) end)
    {
        _service.ActiveSelectionMode = SelectionMode.Column;
        _service.Model.SelectionStart = start;
        _service.Model.SelectionEnd = end;
    }

    // column selection: should select a column of text
    [Fact]
    public void Column_selection_selects_a_column_of_text()
    {
        SetLines("abcdefghij", "klmnopqrst", "uvwxyz");

        SelectColumns((2, 0), (4, 2));

        Assert.Equal("cd\nmn\nwx", _service.SelectionText);
    }

    // column selection: should select a column of text without chopping up double width characters
    [Fact]
    public void Column_selection_does_not_chop_up_double_width_characters()
    {
        const string Go = "語"; // 語
        SetLines("a", Go, "b");

        SelectColumns((0, 0), (1, 2));

        Assert.Equal("a\n" + Go + "\nb", _service.SelectionText);
    }

    // column selection: should select a column of text with single character emojis
    [Fact]
    public void Column_selection_keeps_single_character_emojis()
    {
        SetLines("a", "☃", "c");

        SelectColumns((0, 0), (1, 2));

        Assert.Equal("a\n☃\nc", _service.SelectionText);
    }

    // column selection: should select a column of text with double character emojis
    [Fact]
    public void Column_selection_keeps_double_character_emojis()
    {
        const string Grin = "\U0001F601";
        Buffer.Lines.Length = 3;
        Buffer.Lines.Set(0, StringToRow("a "));
        Buffer.Lines.Set(1, StringArrayToRow([Grin, " "]));
        Buffer.Lines.Set(2, StringToRow("c "));

        SelectColumns((0, 0), (1, 2));

        Assert.Equal("a\n" + Grin + "\nc", _service.SelectionText);
    }

    // _areCoordsInSelection: should return whether coords are in the selection
    [Fact]
    public void AreCoordsInSelection_reports_whether_coords_are_in_the_selection()
    {
        Assert.False(SelectionService.AreCoordsInSelection((0, 0), (2, 0), (2, 1)));
        Assert.False(SelectionService.AreCoordsInSelection((1, 0), (2, 0), (2, 1)));
        Assert.True(SelectionService.AreCoordsInSelection((2, 0), (2, 0), (2, 1)));
        Assert.True(SelectionService.AreCoordsInSelection((10, 0), (2, 0), (2, 1)));
        Assert.True(SelectionService.AreCoordsInSelection((0, 1), (2, 0), (2, 1)));
        Assert.True(SelectionService.AreCoordsInSelection((1, 1), (2, 0), (2, 1)));
        Assert.False(SelectionService.AreCoordsInSelection((2, 1), (2, 0), (2, 1)));
    }
}
