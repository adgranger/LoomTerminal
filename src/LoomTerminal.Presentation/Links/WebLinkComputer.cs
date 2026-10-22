// Ported from xtermjs/xterm.js: addons/addon-web-links/src/WebLinkProvider.ts, WebLinksAddon.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using System.Text.RegularExpressions;
using LoomTerminal.Core.Buffer;

namespace LoomTerminal.Presentation.Links;

/// <summary>Finds <c>http(s)</c> URLs in the buffer, across wrapped lines.</summary>
/// <remarks>Threading: stateless; call it on the thread that owns the buffer (the parser thread).</remarks>
internal static partial class WebLinkComputer
{
    private const int MaxWindowLength = 2048;

    // Everything starting with http:// or https:// up to the first whitespace, `"` or `'`; the repeated end clause keeps
    // a dangling `:` out, and brackets, unsafe characters and final punctuation are excluded (see WebLinksAddon.ts).
    [GeneratedRegex("""(https?|HTTPS?):[/]{2}[^\s"'!*(){}|\\\^<>`]*[^\s"':,.!?{}|\\\^~\[\]`()<>]""")]
    private static partial Regex StrictUrlRegex();

    /// <summary>Finds the links of the wrapped line that holds row <paramref name="y"/>.</summary>
    /// <param name="buffer">The buffer.</param>
    /// <param name="y">1-based buffer row.</param>
    /// <returns>The links, in order.</returns>
    internal static List<DetectedLink> ComputeLinks(IBuffer buffer, int y)
    {
        var (lines, startLineIndex) = GetWindowedLineStrings(y - 1, buffer);
        var line = string.Concat(lines);
        var result = new List<DetectedLink>();
        foreach (Match match in StrictUrlRegex().Matches(line))
        {
            var text = match.Value;

            // check via Uri if the matched text would form a proper url
            if (!IsUrl(text))
            {
                continue;
            }

            // map string positions back to buffer positions; values are 0-based, right side excluding
            var (startY, startX) = MapStrIdx(buffer, startLineIndex, 0, match.Index);
            var (endY, endX) = MapStrIdx(buffer, startY, startX, text.Length);
            if (startY == -1 || startX == -1 || endY == -1 || endX == -1)
            {
                continue;
            }

            // range expects values 1-based right side including, thus +1 except for endX
            result.Add(new DetectedLink(text, new BufferRange(new BufferCellPosition(startX + 1, startY + 1), new BufferCellPosition(endX, endY + 1))));
        }

        return result;
    }

    private static bool IsUrl(string urlString)
    {
        if (!Uri.TryCreate(urlString, UriKind.Absolute, out var url))
        {
            return false;
        }

        var userInfo = url.UserInfo.Length > 0 ? url.UserInfo + "@" : string.Empty;
        var parsedBase = $"{url.Scheme}://{userInfo}{url.Authority}";
        return urlString.StartsWith(parsedBase, StringComparison.OrdinalIgnoreCase);
    }

    private static IBufferLine? GetLine(IBuffer buffer, int index) =>
        index >= 0 && index < buffer.Lines.Length ? buffer.Lines.Get(index) : null;

    // Gets wrapped content lines for the current line index. The top/bottom line expansion stops at whitespaces or
    // length > 2048. Lines are pulled with trimRight=true on purpose, to match urls with early wrapped wide chars; this
    // corrupts the string index for 1:1 backmapping, hence the correction in MapStrIdx.
    private static (List<string> Lines, int TopIndex) GetWindowedLineStrings(int lineIndex, IBuffer buffer)
    {
        var topIdx = lineIndex;
        var bottomIdx = lineIndex;
        var lines = new List<string>();

        if (GetLine(buffer, lineIndex) is { } line)
        {
            var currentContent = line.TranslateToString(true);

            // expand top, stop on whitespaces or length > 2048
            if (line.IsWrapped && (currentContent.Length == 0 || currentContent[0] != ' '))
            {
                var length = 0;
                var above = new List<string>();
                while (GetLine(buffer, --topIdx) is { } top && length < MaxWindowLength)
                {
                    var content = top.TranslateToString(true);
                    length += content.Length;
                    above.Add(content);
                    if (!top.IsWrapped || content.Contains(' ', StringComparison.Ordinal))
                    {
                        break;
                    }
                }

                above.Reverse();
                lines.AddRange(above);
            }

            // append current line
            lines.Add(currentContent);

            // expand bottom, stop on whitespaces or length > 2048
            var bottomLength = 0;
            while (GetLine(buffer, ++bottomIdx) is { IsWrapped: true } bottom && bottomLength < MaxWindowLength)
            {
                var content = bottom.TranslateToString(true);
                bottomLength += content.Length;
                lines.Add(content);
                if (content.Contains(' ', StringComparison.Ordinal))
                {
                    break;
                }
            }
        }

        return (lines, topIdx);
    }

    // Maps a string index back to buffer positions: (lineIndex, columnIndex) 0-based, or (-1, -1) when the lookup ran
    // into a non-existing line.
    private static (int Line, int Column) MapStrIdx(IBuffer buffer, int lineIndex, int rowIndex, int stringIndex)
    {
        var start = rowIndex;
        while (stringIndex != 0)
        {
            if (GetLine(buffer, lineIndex) is not { } line)
            {
                return (-1, -1);
            }

            for (var i = start; i < line.Length; ++i)
            {
                var chars = line.GetString(i);
                var width = line.GetWidth(i);
                if (width != 0)
                {
                    stringIndex -= chars.Length != 0 ? chars.Length : 1;

                    // correct stringIndex for early wrapped wide chars: it only happens at the last cell, whose chars
                    // are '', and when the follow-up line is wrapped and has a wide char at its first cell
                    if (i == line.Length - 1 && chars.Length == 0
                        && GetLine(buffer, lineIndex + 1) is { IsWrapped: true } next
                        && next.GetWidth(0) == 2)
                    {
                        stringIndex += 1;
                    }
                }

                if (stringIndex < 0)
                {
                    return (lineIndex, i);
                }
            }

            lineIndex++;
            start = 0;
        }

        return (lineIndex, start);
    }
}
