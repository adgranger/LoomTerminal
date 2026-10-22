// Ported from xtermjs/xterm.js: src/browser/selection/SelectionModel.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Services;
using LoomTerminal.Presentation.Selection;

namespace LoomTerminal.Presentation.Tests.Selection;

public sealed class SelectionModelTests : IDisposable
{
    private readonly OptionsService _options = new(new TerminalOptions { Cols = 80, Rows = 2 });
    private readonly BufferService _bufferService;
    private readonly SelectionModel _model;

    public SelectionModelTests()
    {
        _bufferService = new BufferService(_options);
        _model = new SelectionModel(_bufferService);
    }

    public void Dispose()
    {
        _bufferService.Dispose();
        _options.Dispose();
    }

    // clearSelection: should clear the final selection
    [Fact]
    public void ClearSelection_clears_the_final_selection()
    {
        _model.SelectionStart = (0, 0);
        _model.SelectionEnd = (10, 2);
        Assert.Equal((0, 0), _model.FinalSelectionStart);
        Assert.Equal((10, 2), _model.FinalSelectionEnd);

        _model.ClearSelection();

        Assert.Null(_model.FinalSelectionStart);
        Assert.Null(_model.FinalSelectionEnd);
    }

    // areSelectionValuesReversed: should return true when the selection end is before selection start
    [Fact]
    public void Values_are_reversed_when_the_selection_end_is_before_the_start()
    {
        _model.SelectionStart = (1, 0);
        _model.SelectionEnd = (0, 0);
        Assert.True(_model.AreSelectionValuesReversed());
        _model.SelectionStart = (10, 2);
        _model.SelectionEnd = (0, 0);
        Assert.True(_model.AreSelectionValuesReversed());
    }

    // areSelectionValuesReversed: should return false when the selection end is after selection start
    [Fact]
    public void Values_are_not_reversed_when_the_selection_end_is_after_the_start()
    {
        _model.SelectionStart = (0, 0);
        _model.SelectionEnd = (1, 0);
        Assert.False(_model.AreSelectionValuesReversed());
        _model.SelectionStart = (0, 0);
        _model.SelectionEnd = (10, 2);
        Assert.False(_model.AreSelectionValuesReversed());
    }

    // onTrim: should trim a portion of the selection when a part of it is trimmed
    [Fact]
    public void A_trim_shifts_the_selection_up()
    {
        _model.SelectionStart = (0, 0);
        _model.SelectionEnd = (10, 2);

        _model.HandleTrim(1);
        Assert.Equal((0, 0), _model.FinalSelectionStart);
        Assert.Equal((10, 1), _model.FinalSelectionEnd);

        _model.HandleTrim(1);
        Assert.Equal((0, 0), _model.FinalSelectionStart);
        Assert.Equal((10, 0), _model.FinalSelectionEnd);
    }

    // onTrim: should clear selection when it is trimmed in its entirety
    [Fact]
    public void A_selection_trimmed_in_its_entirety_is_cleared()
    {
        _model.SelectionStart = (0, 0);
        _model.SelectionEnd = (10, 0);

        var refresh = _model.HandleTrim(1);

        Assert.True(refresh);
        Assert.Null(_model.FinalSelectionStart);
        Assert.Null(_model.FinalSelectionEnd);
    }

    // finalSelectionStart: should return the start of the buffer if select all is active
    [Fact]
    public void The_final_start_is_the_start_of_the_buffer_when_select_all_is_active()
    {
        _model.IsSelectAllActive = true;

        Assert.Equal((0, 0), _model.FinalSelectionStart);
    }

    // finalSelectionStart: should return selection start if there is no selection end
    [Fact]
    public void The_final_start_is_the_start_when_there_is_no_end()
    {
        _model.SelectionStart = (2, 2);

        Assert.Equal((2, 2), _model.FinalSelectionStart);
    }

    // finalSelectionStart: should return selection end if values are reversed
    [Fact]
    public void The_final_start_is_the_end_when_values_are_reversed()
    {
        _model.SelectionStart = (2, 2);
        _model.SelectionEnd = (3, 2);
        Assert.Equal((2, 2), _model.FinalSelectionStart);

        _model.SelectionEnd = (1, 2);
        Assert.Equal((1, 2), _model.FinalSelectionStart);
    }

    // finalSelectionEnd: should return the end of the buffer if select all is active
    [Fact]
    public void The_final_end_is_the_end_of_the_buffer_when_select_all_is_active()
    {
        _model.IsSelectAllActive = true;

        Assert.Equal((80, 1), _model.FinalSelectionEnd);
    }

    // finalSelectionEnd: should return null if there is no selection start
    [Fact]
    public void There_is_no_final_end_without_a_start()
    {
        Assert.Null(_model.FinalSelectionEnd);

        _model.SelectionEnd = (1, 2);

        Assert.Null(_model.FinalSelectionEnd);
    }

    // finalSelectionEnd: should return selection start + length if there is no selection end
    [Fact]
    public void The_final_end_is_start_plus_length_when_there_is_no_end()
    {
        _model.SelectionStart = (2, 2);
        _model.SelectionStartLength = 2;

        Assert.Equal((4, 2), _model.FinalSelectionEnd);
    }

    // finalSelectionEnd: should return selection start + length if values are reversed
    [Fact]
    public void The_final_end_is_start_plus_length_when_values_are_reversed()
    {
        _model.SelectionStart = (2, 2);
        _model.SelectionStartLength = 2;
        _model.SelectionEnd = (2, 1);

        Assert.Equal((4, 2), _model.FinalSelectionEnd);
    }

    // finalSelectionEnd: should return selection start + length if selection end is inside the start selection
    [Fact]
    public void The_final_end_is_start_plus_length_when_the_end_is_inside_the_start_selection()
    {
        _model.SelectionStart = (2, 2);
        _model.SelectionStartLength = 2;
        _model.SelectionEnd = (3, 2);

        Assert.Equal((4, 2), _model.FinalSelectionEnd);
    }

    // finalSelectionEnd: should return the end on a different row when start + length overflows onto a following row
    [Fact]
    public void The_final_end_wraps_onto_the_following_row_when_start_plus_length_overflows()
    {
        _model.SelectionStart = (78, 2);
        _model.SelectionStartLength = 4;

        Assert.Equal((2, 3), _model.FinalSelectionEnd);
    }

    // finalSelectionEnd: ... when start + length overflows onto a following row with selectionEnd inbetween
    [Fact]
    public void The_final_end_wraps_onto_the_following_row_with_an_end_in_between()
    {
        _model.SelectionStart = (78, 2);
        _model.SelectionEnd = (79, 2);
        _model.SelectionStartLength = 4;

        Assert.Equal((2, 3), _model.FinalSelectionEnd);
    }

    // finalSelectionEnd: should return selection end if selection end is after selection start + length
    [Fact]
    public void The_final_end_is_the_end_when_it_is_after_start_plus_length()
    {
        _model.SelectionStart = (2, 2);
        _model.SelectionStartLength = 2;
        _model.SelectionEnd = (5, 2);

        Assert.Equal((5, 2), _model.FinalSelectionEnd);
    }

    // finalSelectionEnd: should not include a trailing EOL when the selection ends at the end of a line
    [Fact]
    public void The_final_end_does_not_include_a_trailing_eol_at_the_end_of_a_line()
    {
        _model.SelectionStart = (0, 0);
        _model.SelectionStartLength = 80;
        Assert.Equal((80, 0), _model.FinalSelectionEnd);

        _model.SelectionStart = (0, 0);
        _model.SelectionStartLength = 160;
        Assert.Equal((80, 1), _model.FinalSelectionEnd);
    }
}
