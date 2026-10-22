// Ported from xtermjs/xterm.js: src/browser/OscLinkProvider.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Buffer;
using LoomTerminal.Core.Services;

namespace LoomTerminal.Presentation.Links;

/// <summary>Finds the OSC 8 hyperlinks of a buffer row; only <c>http</c> and <c>https</c> links, as xterm.js by default.</summary>
/// <remarks>Threading: stateless; call it on the thread that owns the buffer (the parser thread).</remarks>
internal static class OscLinkProvider
{
    /// <summary>Finds the links of row <paramref name="y"/>.</summary>
    /// <param name="buffer">The buffer.</param>
    /// <param name="oscLinkService">The registry of OSC 8 links.</param>
    /// <param name="y">1-based buffer row.</param>
    /// <returns>The links, in order.</returns>
    internal static List<DetectedLink> ProvideLinks(IBuffer buffer, IOscLinkService oscLinkService, int y)
    {
        var result = new List<DetectedLink>();
        if (y < 1 || y > buffer.Lines.Length || buffer.Lines.Get(y - 1) is not { } line)
        {
            return result;
        }

        var cell = new CellData();
        var lineLength = line.GetTrimmedLength();
        var currentLinkId = -1;
        var currentStart = -1;
        var finishLink = false;
        for (var x = 0; x < lineLength; x++)
        {
            // Minor optimization, only check for content if there isn't a link in case the link ends with a null cell
            if (currentStart == -1 && line.HasContent(x) == 0)
            {
                continue;
            }

            line.LoadCell(x, cell);
            if (cell.HasExtendedAttrs() != 0 && cell.Extended.UrlId != 0)
            {
                if (currentStart == -1)
                {
                    currentStart = x;
                    currentLinkId = cell.Extended.UrlId;
                    continue;
                }

                finishLink = cell.Extended.UrlId != currentLinkId;
            }
            else if (currentStart != -1)
            {
                finishLink = true;
            }

            if (finishLink || (currentStart != -1 && x == lineLength - 1))
            {
                var text = oscLinkService.GetLinkData(currentLinkId)?.Uri;
                if (!string.IsNullOrEmpty(text) && IsHttp(text))
                {
                    // These ranges are 1-based; the end is offset when the link ends on the last cell of the line.
                    var end = x + (!finishLink && x == lineLength - 1 ? 1 : 0);
                    result.Add(new DetectedLink(text, new BufferRange(new BufferCellPosition(currentStart + 1, y), new BufferCellPosition(end, y))));
                }

                finishLink = false;

                // Clear link or start a new link if one starts immediately
                if (cell.HasExtendedAttrs() != 0 && cell.Extended.UrlId != 0)
                {
                    currentStart = x;
                    currentLinkId = cell.Extended.UrlId;
                }
                else
                {
                    currentStart = -1;
                    currentLinkId = -1;
                }
            }
        }

        return result;
    }

    // Invalid URLs are ignored to prevent unexpected behaviors.
    private static bool IsHttp(string text) =>
        Uri.TryCreate(text, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
