// Ported from xtermjs/xterm.js: src/browser/Linkifier.ts (logic only: hover, _linkAtPosition)
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core;
using LoomTerminal.Core.Buffer;
using LoomTerminal.Presentation.Input;

namespace LoomTerminal.Presentation.Links;

/// <summary>
/// Tracks the link under the pointer while Ctrl is held and opens it on Ctrl+click. Links the handler refuses are never
/// current. OSC 8 links win over detected ones, as the OSC provider has the higher priority in xterm.js.
/// </summary>
/// <remarks>Threading: parser thread only, except <see cref="Handler"/>, which any thread may set.</remarks>
internal sealed class LinkTracker(TerminalCore core)
{
    private volatile ILinkHandler? _handler = new HttpsOnlyLinkHandler();
    private (int X, int Y)? _downCell;

    /// <summary>The handler; without one, no link is offered.</summary>
    internal ILinkHandler? Handler
    {
        get => _handler;
        set => _handler = value;
    }

    /// <summary>The link under the pointer, when Ctrl is held and the handler accepts it.</summary>
    internal DetectedLink? Current { get; private set; }

    /// <summary>Updates the current link and opens it on a Ctrl+click (press and release on the same link).</summary>
    /// <param name="e">The pointer event.</param>
    internal void Handle(TerminalPointerEvent e)
    {
        var handler = _handler;
        if (e.Action == PointerAction.Wheel)
        {
            return;
        }

        if (handler is null || !e.Ctrl)
        {
            Clear();
            return;
        }

        var buffer = core.Buffers.Active;
        var position = (X: Math.Clamp(e.Column, 0, core.Cols - 1) + 1, Y: Math.Clamp(e.Row, 0, core.Rows - 1) + buffer.YDisp + 1);
        var link = FindLink(buffer, position, handler);
        Current = link;

        if (e.Button != PointerButton.Left)
        {
            return;
        }

        if (e.Action == PointerAction.Down)
        {
            _downCell = position;
        }
        else if (e.Action == PointerAction.Up)
        {
            if (link is { } l && _downCell == position && Uri.TryCreate(l.Text, UriKind.Absolute, out var uri))
            {
                handler.Open(uri);
            }

            _downCell = null;
        }
    }

    /// <summary>Forgets the current link.</summary>
    internal void Clear()
    {
        Current = null;
        _downCell = null;
    }

    // Whether the cell is part of the link: a comparison of linear positions, as Linkifier._linkAtPosition.
    private bool IsAtPosition(DetectedLink link, (int X, int Y) position)
    {
        var cols = core.Cols;
        var lower = (link.Range.Start.Y * cols) + link.Range.Start.X;
        var upper = (link.Range.End.Y * cols) + link.Range.End.X;
        var current = (position.Y * cols) + position.X;
        return lower <= current && current <= upper;
    }

    private DetectedLink? FindLink(IBuffer buffer, (int X, int Y) position, ILinkHandler handler)
    {
        // The first provider with a link here wins, even when its link is refused: the lower priority one is not shown.
        foreach (var candidates in new[]
        {
            OscLinkProvider.ProvideLinks(buffer, core.OscLinkService, position.Y),
            WebLinkComputer.ComputeLinks(buffer, position.Y),
        })
        {
            foreach (var link in candidates)
            {
                if (IsAtPosition(link, position))
                {
                    return Uri.TryCreate(link.Text, UriKind.Absolute, out var uri) && handler.CanOpen(uri) ? link : null;
                }
            }
        }

        return null;
    }
}
