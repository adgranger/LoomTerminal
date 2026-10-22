// Ported from xtermjs/xterm.js: addons/addon-search/src/SearchEngine.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core;
using LoomTerminal.Core.Services;
using LoomTerminal.Presentation.Search;

namespace LoomTerminal.Presentation.Tests.Search;

public sealed class SearchEngineTests : IDisposable
{
    private readonly TerminalCore _terminal = new(new TerminalOptions { Cols = 80, Rows = 24 });
    private readonly SearchBufferView _view;
    private readonly SearchLineCache _lineCache;
    private readonly SearchEngine _engine;

    public SearchEngineTests()
    {
        _view = new SearchBufferView(_terminal);
        _lineCache = new SearchLineCache(_view);
        _engine = new SearchEngine(_view, _lineCache);
    }

    public void Dispose()
    {
        _lineCache.Dispose();
        _terminal.Dispose();
    }

    private static SearchResult R(string term, int col, int row, int size) => new(term, col, row, size);

    // The JS tests replace terminal.getSelectionPosition.
    private void SelectionAt(int startX, int startY, int endX, int endY) =>
        _view.GetSelectionPosition = () => new SearchSelectionPosition((startX, startY), (endX, endY));

    // find: should return undefined for empty search term
    [Fact]
    public void An_empty_term_finds_nothing()
    {
        _terminal.Write("Hello World");

        Assert.Null(_engine.Find(string.Empty, 0, 0));
    }

    // find: should find basic text in terminal content
    [Fact]
    public void Basic_text_is_found()
    {
        _terminal.Write("Hello World");

        Assert.Equal(R("World", 6, 0, 5), _engine.Find("World", 0, 0));
    }

    // find: should find text starting from specified position
    [Fact]
    public void The_search_starts_from_the_given_position()
    {
        _terminal.Write("Hello Hello Hello");

        Assert.Equal(R("Hello", 12, 0, 5), _engine.Find("Hello", 0, 7));
    }

    // find: should search across multiple rows
    [Fact]
    public void The_search_goes_through_the_rows()
    {
        _terminal.Write("Line 1\r\nLine 2 target\r\nLine 3");

        Assert.Equal(R("target", 7, 1, 6), _engine.Find("target", 0, 0));
    }

    // find: should return undefined when text is not found
    [Fact]
    public void Missing_text_is_not_found()
    {
        _terminal.Write("Hello World");

        Assert.Null(_engine.Find("NotFound", 0, 0));
    }

    // find: should throw error for invalid column position
    [Fact]
    public void An_invalid_column_throws()
    {
        _terminal.Write("Hello World");

        var ex = Assert.Throws<ArgumentException>(() => _engine.Find("Hello", 0, 100));

        Assert.Equal("Invalid col: 100 to search in terminal of 80 cols", ex.Message);
    }

    // find: should handle search starting from last column
    [Fact]
    public void A_search_from_the_last_column_finds_nothing()
    {
        _terminal.Write("Hello World");

        Assert.Null(_engine.Find("Hello", 0, 79));
    }

    // find: should handle search from middle of match
    [Fact]
    public void A_match_that_starts_before_the_start_position_is_not_found()
    {
        _terminal.Write("Hello World");

        Assert.Null(_engine.Find("llo", 0, 3));
    }

    // caseSensitive: should find text with case-insensitive search (default)
    [Fact]
    public void The_search_ignores_case_by_default()
    {
        _terminal.Write("Hello WORLD");

        Assert.Equal(R("world", 6, 0, 5), _engine.Find("world", 0, 0));
    }

    // caseSensitive: should find text with case-sensitive search when enabled
    [Fact]
    public void A_case_sensitive_search_finds_the_same_case()
    {
        _terminal.Write("Hello WORLD");

        Assert.Equal(R("WORLD", 6, 0, 5), _engine.Find("WORLD", 0, 0, new SearchOptions { CaseSensitive = true }));
    }

    // caseSensitive: should not find text with case-sensitive search when case differs
    [Fact]
    public void A_case_sensitive_search_does_not_find_another_case()
    {
        _terminal.Write("Hello WORLD");

        Assert.Null(_engine.Find("world", 0, 0, new SearchOptions { CaseSensitive = true }));
    }

    // wholeWord: should find whole word when enabled
    [Fact]
    public void A_whole_word_is_found()
    {
        _terminal.Write("Hello world wonderful");

        Assert.Equal(R("world", 6, 0, 5), _engine.Find("world", 0, 0, new SearchOptions { WholeWord = true }));
    }

    // wholeWord: should not find partial word when wholeWord is enabled
    [Fact]
    public void A_partial_word_is_not_found_with_whole_word()
    {
        _terminal.Write("Hello wonderful");

        Assert.Null(_engine.Find("world", 0, 0, new SearchOptions { WholeWord = true }));
    }

    // wholeWord: should find word at beginning of line with wholeWord
    [Fact]
    public void A_whole_word_at_the_beginning_of_the_line_is_found()
    {
        _terminal.Write("world is great");

        Assert.Equal(R("world", 0, 0, 5), _engine.Find("world", 0, 0, new SearchOptions { WholeWord = true }));
    }

    // wholeWord: should find word at end of line with wholeWord
    [Fact]
    public void A_whole_word_at_the_end_of_the_line_is_found()
    {
        _terminal.Write("hello world");

        Assert.Equal(R("world", 6, 0, 5), _engine.Find("world", 0, 0, new SearchOptions { WholeWord = true }));
    }

    // wholeWord: should handle word boundaries with punctuation
    [Fact]
    public void Punctuation_is_a_word_boundary()
    {
        _terminal.Write("hello,world!test");

        Assert.Equal(R("world", 6, 0, 5), _engine.Find("world", 0, 0, new SearchOptions { WholeWord = true }));
    }

    // wholeWord: should not match when not whole word
    [Fact]
    public void A_word_inside_another_is_not_a_whole_word()
    {
        _terminal.Write("helloworld");

        Assert.Null(_engine.Find("world", 0, 0, new SearchOptions { WholeWord = true }));
    }

    // regex: should find text using simple regex pattern
    [Fact]
    public void A_simple_regex_is_found()
    {
        _terminal.Write("Hello 123 World");

        Assert.Equal(R("123", 6, 0, 3), _engine.Find("[0-9]+", 0, 0, new SearchOptions { Regex = true }));
    }

    // regex: should find text using regex with case-insensitive flag
    [Fact]
    public void A_regex_ignores_case_when_asked()
    {
        _terminal.Write("Hello WORLD");

        Assert.Equal(R("WORLD", 6, 0, 5), _engine.Find("world", 0, 0, new SearchOptions { Regex = true, CaseSensitive = false }));
    }

    // regex: should find text using regex with case-sensitive flag
    [Fact]
    public void A_case_sensitive_regex_finds_the_same_case()
    {
        _terminal.Write("Hello WORLD world");

        Assert.Equal(R("WORLD", 6, 0, 5), _engine.Find("WORLD", 0, 0, new SearchOptions { Regex = true, CaseSensitive = true }));
    }

    // regex: should handle complex regex patterns
    [Fact]
    public void A_complex_regex_is_found()
    {
        _terminal.Write("Email: test@example.com and another@domain.org");

        Assert.Equal(
            R("test@example.com", 7, 0, 16),
            _engine.Find("[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}", 0, 0, new SearchOptions { Regex = true }));
    }

    // regex: should return undefined for invalid regex pattern (JS throws SyntaxError; the port finds nothing, P3-18)
    [Fact]
    public void An_invalid_regex_finds_nothing()
    {
        _terminal.Write("Hello World");

        Assert.Null(_engine.Find("[invalid", 0, 0, new SearchOptions { Regex = true }));
    }

    // regex: should handle empty regex matches
    [Fact]
    public void Empty_regex_matches_are_ignored()
    {
        _terminal.Write("Hello World");

        Assert.Null(_engine.Find(".*?", 0, 0, new SearchOptions { Regex = true }));
    }

    // combined options: should handle regex + caseSensitive combination
    [Fact]
    public void A_case_sensitive_regex_matches_the_first_capital()
    {
        _terminal.Write("Hello WORLD world");

        Assert.Equal(R("H", 0, 0, 1), _engine.Find("[A-Z]+", 0, 0, new SearchOptions { Regex = true, CaseSensitive = true }));
    }

    // combined options: should handle wholeWord + caseSensitive combination
    [Fact]
    public void Whole_word_and_case_sensitive_combine()
    {
        _terminal.Write("Hello WORLD wonderful");

        Assert.Equal(R("WORLD", 6, 0, 5), _engine.Find("WORLD", 0, 0, new SearchOptions { WholeWord = true, CaseSensitive = true }));
        Assert.Null(_engine.Find("world", 0, 0, new SearchOptions { WholeWord = true, CaseSensitive = true }));
    }

    // Not in the JS tests: a regex that backtracks forever finds nothing instead of hanging (1 s limit, P3-18).
    [Fact]
    public void A_catastrophic_regex_finds_nothing()
    {
        _terminal.Write(new string('a', 30) + "!");

        Assert.Null(_engine.Find("(a+)+$", 0, 0, new SearchOptions { Regex = true }));
    }

    // findNextWithSelection: should return undefined for empty search term
    [Fact]
    public void Find_next_with_an_empty_term_finds_nothing()
    {
        _terminal.Write("Hello World");

        Assert.Null(_engine.FindNextWithSelection(string.Empty));
    }

    // findNextWithSelection: should find first occurrence when no selection exists
    [Fact]
    public void Find_next_without_selection_finds_the_first_occurrence()
    {
        _terminal.Write("Hello World Hello");

        Assert.Equal(R("Hello", 0, 0, 5), _engine.FindNextWithSelection("Hello"));
    }

    // findNextWithSelection: should find next occurrence after current selection
    [Fact]
    public void Find_next_goes_after_the_selection()
    {
        _terminal.Write("Hello World Hello Again");
        SelectionAt(0, 0, 5, 0);

        Assert.Equal(R("Hello", 12, 0, 5), _engine.FindNextWithSelection("Hello", null, "Hello"));
    }

    // findNextWithSelection: should wrap around to beginning when reaching end
    [Fact]
    public void Find_next_wraps_to_the_beginning()
    {
        _terminal.Write("Hello World Hello");
        SelectionAt(12, 0, 17, 0);

        Assert.Equal(R("Hello", 0, 0, 5), _engine.FindNextWithSelection("Hello", null, "Hello"));
    }

    // findNextWithSelection: should wrap across multiple rows
    [Fact]
    public void Find_next_wraps_across_rows()
    {
        _terminal.Write("Line 1 test\r\nLine 2\r\nLine 3 test");
        SelectionAt(7, 0, 11, 0);

        Assert.Equal(R("test", 7, 2, 4), _engine.FindNextWithSelection("test", null, "test"));
    }

    // findNextWithSelection: should return same selection if only one match exists
    [Fact]
    public void Find_next_returns_the_selection_when_it_is_the_only_match()
    {
        _terminal.Write("Hello World");
        SelectionAt(0, 0, 5, 0);

        Assert.Equal(R("Hello", 0, 0, 5), _engine.FindNextWithSelection("Hello"));
    }

    // findNextWithSelection: should clear selection and return undefined when term not found
    [Fact]
    public void Find_next_returns_null_when_the_term_is_missing()
    {
        _terminal.Write("Hello World");

        Assert.Null(_engine.FindNextWithSelection("NotFound"));
    }

    // findPreviousWithSelection: should return undefined for empty search term
    [Fact]
    public void Find_previous_with_an_empty_term_finds_nothing()
    {
        _terminal.Write("Hello World");

        Assert.Null(_engine.FindPreviousWithSelection(string.Empty));
    }

    // findPreviousWithSelection: should find last occurrence when no selection exists
    [Fact]
    public void Find_previous_without_selection_finds_the_last_occurrence()
    {
        _terminal.Write("Hello World Hello");

        Assert.Equal(R("Hello", 12, 0, 5), _engine.FindPreviousWithSelection("Hello"));
    }

    // findPreviousWithSelection: should find previous occurrence before current selection,
    // should wrap around to end when reaching beginning, should work across multiple rows in reverse,
    // should handle selection expansion correctly (the JS tests only check the row of the result)
    [Theory]
    [InlineData("Hello World Hello Again", "Hello", 12, 0, 17, 0, 0)]
    [InlineData("Hello World Hello", "Hello", 0, 0, 5, 0, 0)]
    [InlineData("test Line 1\r\nLine 2\r\ntest Line 3", "test", 0, 2, 4, 2, 2)]
    public void Find_previous_finds_a_match_near_the_selection(string text, string term, int startX, int startY, int endX, int endY, int expectedRow)
    {
        _terminal.Write(text);
        SelectionAt(startX, startY, endX, endY);

        var result = _engine.FindPreviousWithSelection(term);

        Assert.NotNull(result);
        Assert.Equal(expectedRow, result.Row);
    }

    // findPreviousWithSelection: should clear selection and return undefined when term not found
    [Fact]
    public void Find_previous_returns_null_when_the_term_is_missing()
    {
        _terminal.Write("Hello World");

        Assert.Null(_engine.FindPreviousWithSelection("NotFound"));
    }

    // unicode and special characters: should handle unicode characters correctly
    [Fact]
    public void Unicode_characters_are_found()
    {
        _terminal.Write("Hello 世界 World");

        Assert.Equal(R("世界", 6, 0, 4), _engine.Find("世界", 0, 0));
    }

    // unicode and special characters: should handle wide characters
    [Fact]
    public void Wide_characters_are_found()
    {
        _terminal.Write("中文测试");

        Assert.Equal(R("测试", 4, 0, 4), _engine.Find("测试", 0, 0));
    }

    // wrapped lines: should handle search across wrapped lines
    [Fact]
    public void A_search_goes_across_wrapped_lines()
    {
        _terminal.Write(new string('A', 100) + "target" + new string('B', 50));

        Assert.Equal(R("target", 20, 1, 6), _engine.Find("target", 0, 0));
    }

    // wrapped lines: should handle wrapped lines with unicode
    [Fact]
    public void A_search_goes_across_wrapped_lines_with_wide_characters()
    {
        _terminal.Write(new string('中', 50) + "target" + new string('文', 30));

        Assert.Equal(R("target", 20, 1, 6), _engine.Find("target", 0, 0));
    }

    // wrapped lines: should skip wrapped lines correctly in findInLine
    [Fact]
    public void Wrapped_rows_are_skipped_in_find_in_line()
    {
        _terminal.Write(new string('A', 200) + "\r\nNext line with target");

        Assert.Equal(R("target", 15, 3, 6), _engine.Find("target", 0, 0));
    }

    // buffer boundaries: should handle empty buffer gracefully
    [Fact]
    public void An_empty_buffer_finds_nothing() => Assert.Null(_engine.Find("anything", 0, 0));

    // buffer boundaries: should handle search beyond buffer size
    [Fact]
    public void A_search_beyond_the_buffer_finds_nothing() => Assert.Null(_engine.Find("test", 1000, 0));

    // invalid inputs: should handle undefined search options gracefully
    [Fact]
    public void Missing_options_are_the_defaults()
    {
        _terminal.Write("Hello World");

        Assert.Equal(R("Hello", 0, 0, 5), _engine.Find("Hello", 0, 0, null));
    }

    // invalid inputs: should handle negative start positions
    [Fact]
    public void Negative_start_positions_are_accepted()
    {
        _terminal.Write("Hello World");

        Assert.Equal(R("Hello", 0, 0, 5), _engine.Find("Hello", -1, -1));
    }

    // _isWholeWord behavior: should recognize word boundaries with various punctuation
    [Theory]
    [InlineData("word1")]
    [InlineData("word2")]
    [InlineData("word3")]
    [InlineData("word4")]
    [InlineData("word5")]
    [InlineData("word6")]
    [InlineData("word7")]
    [InlineData("word8")]
    public void Brackets_and_commas_are_word_boundaries(string term)
    {
        _terminal.Write("word1 word2,word3(word4)word5[word6]word7{word8}");

        Assert.NotNull(_engine.Find(term, 0, 0, new SearchOptions { WholeWord = true }));
    }

    // _isWholeWord behavior: should handle word boundaries at line start and end
    [Fact]
    public void Word_boundaries_at_line_start_and_end()
    {
        _terminal.Write("start middle end");

        Assert.Equal(R("start", 0, 0, 5), _engine.Find("start", 0, 0, new SearchOptions { WholeWord = true }));
        Assert.Equal(R("end", 13, 0, 3), _engine.Find("end", 0, 0, new SearchOptions { WholeWord = true }));
        Assert.Equal(R("middle", 6, 0, 6), _engine.Find("middle", 0, 0, new SearchOptions { WholeWord = true }));
    }

    // buffer offset calculations: should handle wide character offset calculations
    [Fact]
    public void Wide_characters_before_the_match_are_counted()
    {
        _terminal.Write("中文 test 测试");

        var result = _engine.Find("test", 0, 0);

        Assert.NotNull(result);
        Assert.Equal("test", result.Term);
    }

    // string to buffer size conversions: should correctly calculate size for simple text
    [Fact]
    public void The_size_of_simple_text_is_its_length()
    {
        _terminal.Write("Hello World");

        Assert.Equal(R("World", 6, 0, 5), _engine.Find("World", 0, 0));
    }

    // string to buffer size conversions: should correctly calculate size for unicode text
    [Fact]
    public void The_size_of_wide_text_is_at_least_its_length()
    {
        _terminal.Write("Hello 世界");

        var result = _engine.Find("世界", 0, 0);

        Assert.NotNull(result);
        Assert.True(result.Size >= 2);
    }

    // string to buffer size conversions: should handle size calculation across wrapped lines
    [Fact]
    public void The_size_across_wrapped_lines_covers_the_whole_match()
    {
        var longMatch = new string('A', 100);
        _terminal.Write(longMatch);

        var result = _engine.Find(longMatch, 0, 0);

        Assert.NotNull(result);
        Assert.True(result.Size >= 100);
    }

    // integration with SearchLineCache: should use cache for line translation
    [Fact]
    public void A_cached_search_gives_the_same_result()
    {
        _terminal.Write("Hello World");
        _lineCache.InitLinesCache();

        var result1 = _engine.Find("World", 0, 0);
        var result2 = _engine.Find("World", 0, 0);

        Assert.NotNull(result1);
        Assert.Equal(result1, result2);
    }

    // integration with SearchLineCache: should handle cache misses gracefully
    [Fact]
    public void A_search_without_a_prepared_cache_works()
    {
        _terminal.Write("Hello World");

        Assert.Equal(R("World", 6, 0, 5), _engine.Find("World", 0, 0));
    }

    // integration with SearchLineCache: should work correctly with cache invalidation
    [Fact]
    public void New_content_invalidates_the_cache()
    {
        _terminal.Write("Initial text");
        _lineCache.InitLinesCache();
        Assert.NotNull(_engine.Find("Initial", 0, 0));

        _terminal.Write("\r\nNew line");

        Assert.Equal(R("New", 0, 1, 3), _engine.Find("New", 0, 0));
    }
}
