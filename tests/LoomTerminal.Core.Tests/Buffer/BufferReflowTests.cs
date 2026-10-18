// Ported from xtermjs/xterm.js: src/common/buffer/BufferReflow.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Buffer;

namespace LoomTerminal.Core.Tests.Buffer;

public class BufferReflowTests
{
    private static readonly CharData Empty = new(0, string.Empty, 0, 0);

    private static CharData Wide(string c) => new(0, c, 2, c[0]);

    private static CharData Single(string c) => new(0, c, 1, c[0]);

    // xterm.js: 'BufferReflow' › 'reflowSmallerGetNewLineLengths' › 'should return correct line lengths for a small line with wide characters'
    [Fact]
    public void Small_line_with_wide_characters()
    {
        BufferLine line = new(4);
        line.Set(0, Wide("汉"));
        line.Set(1, Empty);
        line.Set(2, Wide("语"));
        line.Set(3, Empty);
        Assert.Equal("汉语", line.TranslateToString(true));
        Assert.Equal([2, 2], BufferReflow.ReflowSmallerGetNewLineLengths([line], 4, 3)); // line: 汉, 语
        Assert.Equal([2, 2], BufferReflow.ReflowSmallerGetNewLineLengths([line], 4, 2)); // line: 汉, 语
    }

    // xterm.js: 'BufferReflow' › 'reflowSmallerGetNewLineLengths' › 'should return correct line lengths for a large line with wide characters'
    [Fact]
    public void Large_line_with_wide_characters()
    {
        BufferLine line = new(12);
        for (int i = 0; i < 12; i += 4)
        {
            line.Set(i, Wide("汉"));
            line.Set(i + 2, Wide("语"));
        }

        for (int i = 1; i < 12; i += 2)
        {
            line.Set(i, Empty);
            line.Set(i, Empty);
        }

        Assert.Equal("汉语汉语汉语", line.TranslateToString());
        Assert.Equal([10, 2], BufferReflow.ReflowSmallerGetNewLineLengths([line], 12, 11)); // line: 汉语汉语汉, 语
        Assert.Equal([10, 2], BufferReflow.ReflowSmallerGetNewLineLengths([line], 12, 10)); // line: 汉语汉语汉, 语
        Assert.Equal([8, 4], BufferReflow.ReflowSmallerGetNewLineLengths([line], 12, 9)); // line: 汉语汉语, 汉语
        Assert.Equal([8, 4], BufferReflow.ReflowSmallerGetNewLineLengths([line], 12, 8)); // line: 汉语汉语, 汉语
        Assert.Equal([6, 6], BufferReflow.ReflowSmallerGetNewLineLengths([line], 12, 7)); // line: 汉语汉, 语汉语
        Assert.Equal([6, 6], BufferReflow.ReflowSmallerGetNewLineLengths([line], 12, 6)); // line: 汉语汉, 语汉语
        Assert.Equal([4, 4, 4], BufferReflow.ReflowSmallerGetNewLineLengths([line], 12, 5)); // line: 汉语, 汉语, 汉语
        Assert.Equal([4, 4, 4], BufferReflow.ReflowSmallerGetNewLineLengths([line], 12, 4)); // line: 汉语, 汉语, 汉语
        Assert.Equal([2, 2, 2, 2, 2, 2], BufferReflow.ReflowSmallerGetNewLineLengths([line], 12, 3)); // line: 汉, 语, 汉, 语, 汉, 语
        Assert.Equal([2, 2, 2, 2, 2, 2], BufferReflow.ReflowSmallerGetNewLineLengths([line], 12, 2)); // line: 汉, 语, 汉, 语, 汉, 语
    }

    private static BufferLine MixedLine(bool isWrapped)
    {
        BufferLine line = new(6, null, isWrapped);
        line.Set(0, Single("a"));
        line.Set(1, Wide("汉"));
        line.Set(2, Empty);
        line.Set(3, Wide("语"));
        line.Set(4, Empty);
        line.Set(5, Single("b"));
        return line;
    }

    // xterm.js: 'BufferReflow' › 'reflowSmallerGetNewLineLengths' › 'should return correct line lengths for a string with wide and single characters'
    [Fact]
    public void String_with_wide_and_single_characters()
    {
        BufferLine line = MixedLine(false);
        Assert.Equal("a汉语b", line.TranslateToString());
        Assert.Equal([5, 1], BufferReflow.ReflowSmallerGetNewLineLengths([line], 6, 5)); // line: a汉语b
        Assert.Equal([3, 3], BufferReflow.ReflowSmallerGetNewLineLengths([line], 6, 4)); // line: a汉, 语b
        Assert.Equal([3, 3], BufferReflow.ReflowSmallerGetNewLineLengths([line], 6, 3)); // line: a汉, 语b
        Assert.Equal([1, 2, 2, 1], BufferReflow.ReflowSmallerGetNewLineLengths([line], 6, 2)); // line: a, 汉, 语, b
    }

    // xterm.js: 'BufferReflow' › 'reflowSmallerGetNewLineLengths' › 'should return correct line lengths for a wrapped line with wide and single characters'
    [Fact]
    public void Wrapped_line_with_wide_and_single_characters()
    {
        BufferLine line1 = MixedLine(false);
        BufferLine line2 = MixedLine(true);
        Assert.Equal("a汉语b", line1.TranslateToString());
        Assert.Equal("a汉语b", line2.TranslateToString());
        Assert.Equal([5, 4, 3], BufferReflow.ReflowSmallerGetNewLineLengths([line1, line2], 6, 5)); // lines: a汉语, ba汉, 语b
        Assert.Equal([3, 4, 4, 1], BufferReflow.ReflowSmallerGetNewLineLengths([line1, line2], 6, 4)); // lines: a汉, 语ba, 汉语, b
        Assert.Equal([3, 3, 3, 3], BufferReflow.ReflowSmallerGetNewLineLengths([line1, line2], 6, 3)); // lines: a汉, 语b, a汉, 语b
        Assert.Equal([1, 2, 2, 2, 2, 2, 1], BufferReflow.ReflowSmallerGetNewLineLengths([line1, line2], 6, 2)); // lines: a, 汉, 语, ba, 汉, 语, b
    }

    // xterm.js: 'BufferReflow' › 'reflowSmallerGetNewLineLengths' › 'should work on lines ending in null space'
    [Fact]
    public void Lines_ending_in_null_space()
    {
        BufferLine line = new(5);
        line.Set(0, Wide("汉"));
        line.Set(1, Empty);
        line.Set(2, Wide("语"));
        line.Set(3, Empty);
        line.Set(4, new CharData(0, BufferConstants.NullCellChar, BufferConstants.NullCellWidth, BufferConstants.NullCellCode));
        Assert.Equal("汉语", line.TranslateToString(true));
        Assert.Equal("汉语 ", line.TranslateToString(false));
        Assert.Equal([2, 2], BufferReflow.ReflowSmallerGetNewLineLengths([line], 4, 3)); // line: 汉, 语
        Assert.Equal([2, 2], BufferReflow.ReflowSmallerGetNewLineLengths([line], 4, 2)); // line: 汉, 语
    }
}
