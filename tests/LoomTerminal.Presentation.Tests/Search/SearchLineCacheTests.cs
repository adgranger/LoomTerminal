// Ported from xtermjs/xterm.js: addons/addon-search/src/SearchLineCache.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core;
using LoomTerminal.Core.Services;
using LoomTerminal.Presentation.Search;

namespace LoomTerminal.Presentation.Tests.Search;

public sealed class SearchLineCacheTests : IDisposable
{
    private readonly TerminalCore _terminal = new(new TerminalOptions { Cols = 80, Rows = 24 });
    private readonly SearchLineCache _cache;

    public SearchLineCacheTests()
    {
        _cache = new SearchLineCache(new SearchBufferView(_terminal));
    }

    public void Dispose()
    {
        _cache.Dispose();
        _terminal.Dispose();
    }

    private static LineCacheEntry Entry(string text, params int[] offsets) => new(text, offsets);

    private static void AssertEntry(LineCacheEntry expected, LineCacheEntry? actual)
    {
        Assert.NotNull(actual);
        Assert.Equal(expected.Text, actual.Value.Text);
        Assert.Equal(expected.Offsets, actual.Value.Offsets);
    }

    // constructor: should start with no cache initialized
    [Fact]
    public void A_new_cache_is_empty() => Assert.Null(_cache.GetLineFromCache(0));

    // initLinesCache: should initialize the lines cache array
    [Fact]
    public void Initializing_the_cache_leaves_it_empty()
    {
        _cache.InitLinesCache();

        Assert.Null(_cache.GetLineFromCache(0));
    }

    // initLinesCache: should not reinitialize if cache already exists
    [Fact]
    public void Initializing_twice_keeps_the_entries()
    {
        _cache.InitLinesCache();
        _cache.SetLineInCache(0, Entry("test", 0));

        _cache.InitLinesCache();

        AssertEntry(Entry("test", 0), _cache.GetLineFromCache(0));
    }

    // getLineFromCache: should return undefined when cache is not initialized / for unset entries
    [Fact]
    public void Unset_entries_are_null()
    {
        Assert.Null(_cache.GetLineFromCache(0));
        Assert.Null(_cache.GetLineFromCache(10));
        _cache.InitLinesCache();
        Assert.Null(_cache.GetLineFromCache(0));
        Assert.Null(_cache.GetLineFromCache(50));
    }

    // getLineFromCache: should return cached entries; setLineInCache: should set entries when cache is initialized
    [Fact]
    public void Cached_entries_are_returned()
    {
        _cache.InitLinesCache();
        _cache.SetLineInCache(5, Entry("test content", 0));

        AssertEntry(Entry("test content", 0), _cache.GetLineFromCache(5));
    }

    // setLineInCache: should not set entries when cache is not initialized
    [Fact]
    public void Entries_are_not_set_when_the_cache_is_not_initialized()
    {
        _cache.SetLineInCache(0, Entry("test content", 0));

        Assert.Null(_cache.GetLineFromCache(0));
    }

    // setLineInCache: should overwrite existing entries
    [Fact]
    public void Entries_are_overwritten()
    {
        _cache.InitLinesCache();
        _cache.SetLineInCache(0, Entry("first content", 0));
        AssertEntry(Entry("first content", 0), _cache.GetLineFromCache(0));

        _cache.SetLineInCache(0, Entry("second content", 0));

        AssertEntry(Entry("second content", 0), _cache.GetLineFromCache(0));
    }

    // translateBufferLineToStringWithWrap: should translate a single line without wrapping
    [Fact]
    public void A_single_line_is_translated()
    {
        _terminal.Write("Hello World");

        AssertEntry(Entry("Hello World", 0), _cache.TranslateBufferLineToStringWithWrap(0, true));
    }

    // translateBufferLineToStringWithWrap: should handle trimRight parameter
    [Fact]
    public void Trim_right_is_honored()
    {
        _terminal.Write("Hello World   ");

        var trimmed = _cache.TranslateBufferLineToStringWithWrap(0, true);
        var notTrimmed = _cache.TranslateBufferLineToStringWithWrap(0, false);

        Assert.Equal("Hello World", trimmed.Text.TrimEnd());
        Assert.StartsWith("Hello World   ", notTrimmed.Text, StringComparison.Ordinal);
        Assert.True(notTrimmed.Text.Length > trimmed.Text.Length);
    }

    // translateBufferLineToStringWithWrap: should handle wrapped lines
    [Fact]
    public void Wrapped_lines_are_joined()
    {
        var longText = new string('A', 200);
        _terminal.Write(longText);

        var result = _cache.TranslateBufferLineToStringWithWrap(0, true);

        Assert.Equal(longText, result.Text);
        Assert.True(result.Offsets.Length > 1);
        Assert.Equal(0, result.Offsets[0]);
    }

    // translateBufferLineToStringWithWrap: should handle wide characters
    [Fact]
    public void Wide_characters_are_translated()
    {
        _terminal.Write("Hello 世界");

        AssertEntry(Entry("Hello 世界", 0), _cache.TranslateBufferLineToStringWithWrap(0, true));
    }

    // translateBufferLineToStringWithWrap: should handle empty lines / lines beyond buffer
    [Theory]
    [InlineData(0)]
    [InlineData(1000)]
    public void Empty_lines_and_lines_beyond_the_buffer_are_empty(int line) =>
        AssertEntry(Entry(string.Empty, 0), _cache.TranslateBufferLineToStringWithWrap(line, true));

    // translateBufferLineToStringWithWrap: should handle complex wrapped content
    [Fact]
    public void Several_lines_are_translated_one_by_one()
    {
        _terminal.Write("Line 1\r\n");
        _terminal.Write("Line 2 with some longer content that might wrap\r\n");
        _terminal.Write("Line 3");

        Assert.Equal("Line 1", _cache.TranslateBufferLineToStringWithWrap(0, true).Text);
        Assert.Equal("Line 2 with some longer content that might wrap", _cache.TranslateBufferLineToStringWithWrap(1, true).Text);
        Assert.Equal("Line 3", _cache.TranslateBufferLineToStringWithWrap(2, true).Text);
    }

    // cache invalidation: should invalidate cache on line feed
    [Fact]
    public void A_line_feed_invalidates_the_cache()
    {
        _cache.InitLinesCache();
        _cache.SetLineInCache(0, Entry("test", 0));
        AssertEntry(Entry("test", 0), _cache.GetLineFromCache(0));

        _terminal.Write("test\r\n");

        Assert.Null(_cache.GetLineFromCache(0));
    }

    // cache invalidation: should invalidate cache on cursor move
    [Fact]
    public void A_cursor_move_invalidates_the_cache()
    {
        _cache.InitLinesCache();
        _cache.SetLineInCache(0, Entry("test", 0));

        _terminal.Write("some text");

        Assert.Null(_cache.GetLineFromCache(0));
    }

    // cache invalidation: should invalidate cache on resize
    [Fact]
    public void A_resize_invalidates_the_cache()
    {
        _cache.InitLinesCache();
        _cache.SetLineInCache(0, Entry("test", 0));

        _terminal.Resize(100, 30);

        Assert.Null(_cache.GetLineFromCache(0));
    }

    // Replaces the time-to-live of xterm.js (LINES_CACHE_TIME_TO_LIVE): the end of the search session drops the cache.
    [Fact]
    public void Ending_the_session_drops_the_cache()
    {
        _cache.InitLinesCache();
        _cache.SetLineInCache(0, Entry("test", 0));

        _cache.EndSession();

        Assert.Null(_cache.GetLineFromCache(0));
    }

    // disposal: should clean up resources on dispose; should be safe to dispose multiple times
    [Fact]
    public void Disposing_drops_the_cache_and_is_repeatable()
    {
        _cache.InitLinesCache();
        _cache.SetLineInCache(0, Entry("test", 0));

        _cache.Dispose();
        _cache.Dispose();

        Assert.Null(_cache.GetLineFromCache(0));
    }

    // LineCacheEntry type: should handle complex line offsets
    [Fact]
    public void Complex_line_offsets_are_kept()
    {
        var entry = Entry("A very long line that wraps multiple times across several terminal lines", 0, 20, 40, 60);
        _cache.InitLinesCache();
        _cache.SetLineInCache(0, entry);

        var retrieved = _cache.GetLineFromCache(0);

        AssertEntry(entry, retrieved);
        Assert.Equal(72, retrieved!.Value.Text.Length);
        Assert.Equal(4, retrieved.Value.Offsets.Length);
    }

    // LineCacheEntry type: should handle unicode characters in cache entries
    [Fact]
    public void Unicode_entries_are_kept()
    {
        var entry = Entry("Hello 世界 🌍 测试", 0);
        _cache.InitLinesCache();
        _cache.SetLineInCache(0, entry);

        AssertEntry(entry, _cache.GetLineFromCache(0));
    }

    // integration with real terminal content: should correctly translate real buffer content
    [Fact]
    public void The_translation_equals_the_direct_translation()
    {
        _terminal.Write("Hello World");

        var cached = _cache.TranslateBufferLineToStringWithWrap(0, true);

        Assert.Equal(_terminal.Buffers.Active.Lines.Get(0)!.TranslateToString(true), cached.Text);
    }

    // integration with real terminal content: should handle real wrapped content correctly
    [Fact]
    public void Real_wrapped_content_is_joined()
    {
        const string LongContent = "This is a very long line that will definitely wrap around in an 80 column terminal and should be handled correctly by the cache";
        _terminal.Write(LongContent);

        var result = _cache.TranslateBufferLineToStringWithWrap(0, true);

        Assert.Equal(LongContent, result.Text);
        Assert.True(result.Offsets.Length > 1);
    }

    // integration with real terminal content: should work with real escape sequences
    [Fact]
    public void Escape_sequences_do_not_reach_the_text()
    {
        _terminal.Write("Before\u001b[31mRed Text\u001b[0mAfter");

        Assert.Equal("BeforeRed TextAfter", _cache.TranslateBufferLineToStringWithWrap(0, true).Text);
    }
}
