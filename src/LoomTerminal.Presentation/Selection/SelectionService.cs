// Ported from xtermjs/xterm.js: src/browser/services/SelectionService.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using System.Text;
using LoomTerminal.Core.Buffer;
using LoomTerminal.Core.Services;
using LoomTerminal.Presentation.Input;

namespace LoomTerminal.Presentation.Selection;

/// <summary>A selection mode, this drives how the selection behaves on mouse move.</summary>
internal enum SelectionMode
{
    /// <summary>Character by character.</summary>
    Normal,

    /// <summary>Word by word.</summary>
    Word,

    /// <summary>Line by line.</summary>
    Line,

    /// <summary>A rectangle of columns.</summary>
    Column,
}

/// <summary>
/// Manages the selection of the terminal: with the help of <see cref="SelectionModel"/> it deals with everything
/// that is not mouse handling or drawing (text of the selection with wide characters, word and line selection, select
/// all, trimming of the scrollback). Pointer gestures call the primitives it exposes.
/// </summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal sealed partial class SelectionService : IDisposable
{
    private const char NonBreakingSpace = ' ';

    private readonly IBufferService _bufferService;
    private readonly IOptionsService _optionsService;
    private readonly bool _isWindows;
    private readonly bool _isMac;
    private readonly List<IDisposable> _subscriptions = [];
    private readonly CellData _workCell = new();
    private IDisposable _trimListener;
    private bool _enabled = true;

    /// <summary>Creates the service over the buffers of <paramref name="bufferService"/>.</summary>
    /// <param name="bufferService">The buffers.</param>
    /// <param name="coreService">Raises the user input event, which clears the selection.</param>
    /// <param name="optionsService">Options (the word separators).</param>
    /// <param name="isWindows">Whether lines of the selection text are joined with CRLF (Windows) or LF.</param>
    /// <param name="isMac">Whether the host is macOS.</param>
    internal SelectionService(
        IBufferService bufferService,
        ICoreService coreService,
        IOptionsService optionsService,
        bool isWindows,
        bool isMac = false)
    {
        _bufferService = bufferService;
        _optionsService = optionsService;
        _isWindows = isWindows;
        _isMac = isMac;
        Model = new SelectionModel(bufferService);
        ActiveSelectionMode = SelectionMode.Normal;

        _subscriptions.Add(coreService.OnUserInput.Subscribe(_ =>
        {
            if (HasSelection)
            {
                ClearSelection();
            }
        }));
        _trimListener = bufferService.Buffer.Lines.OnTrim.Subscribe(HandleTrim);
        _subscriptions.Add(bufferService.Buffers.OnBufferActivate.Subscribe(HandleBufferActivate));

        // Clear selection when resizing vertically (xterm.js issue 5300).
        _subscriptions.Add(bufferService.OnResize.Subscribe(e =>
        {
            if (e.RowsChanged)
            {
                ClearSelection();
            }
        }));
    }

    /// <summary>Raised after the selection changed.</summary>
    internal event Action? SelectionChanged;

    /// <summary>The selection model.</summary>
    internal SelectionModel Model { get; }

    /// <summary>The current selection mode.</summary>
    internal SelectionMode ActiveSelectionMode { get; set; }

    /// <summary>Whether selection is enabled.</summary>
    internal bool IsEnabled => _enabled;

    /// <summary>The final start of the selection, in buffer coordinates.</summary>
    internal (int X, int Y)? SelectionStart => Model.FinalSelectionStart;

    /// <summary>The final end of the selection, in buffer coordinates.</summary>
    internal (int X, int Y)? SelectionEnd => Model.FinalSelectionEnd;

    /// <summary>Whether there is an active text selection.</summary>
    internal bool HasSelection =>
        Model.FinalSelectionStart is { } start && Model.FinalSelectionEnd is { } end && (start.X != end.X || start.Y != end.Y);

    /// <summary>The text currently selected.</summary>
    internal string SelectionText
    {
        get
        {
            if (Model.FinalSelectionStart is not { } start || Model.FinalSelectionEnd is not { } end)
            {
                return string.Empty;
            }

            var buffer = _bufferService.Buffer;
            var result = new List<string>();

            if (ActiveSelectionMode == SelectionMode.Column)
            {
                // Ignore zero width selections
                if (start.X == end.X)
                {
                    return string.Empty;
                }

                // For column selection it's not enough to rely on final selection's swapping of reversed
                // values, it also needs the x coordinates to swap independently of the y coordinate is needed
                var startCol = start.X < end.X ? start.X : end.X;
                var endCol = start.X < end.X ? end.X : start.X;
                for (var i = start.Y; i <= end.Y; i++)
                {
                    result.Add(buffer.TranslateBufferLineToString(i, true, startCol, endCol));
                }
            }
            else
            {
                // Get first row
                var startRowEndCol = start.Y == end.Y ? end.X : -1;
                result.Add(buffer.TranslateBufferLineToString(start.Y, true, start.X, startRowEndCol));

                // Get middle rows
                for (var i = start.Y + 1; i <= end.Y - 1; i++)
                {
                    var bufferLine = buffer.Lines.Get(i);
                    var lineText = buffer.TranslateBufferLineToString(i, true);
                    if (bufferLine?.IsWrapped == true)
                    {
                        result[^1] += lineText;
                    }
                    else
                    {
                        result.Add(lineText);
                    }
                }

                // Get final row
                if (start.Y != end.Y)
                {
                    var bufferLine = buffer.Lines.Get(end.Y);
                    var lineText = buffer.TranslateBufferLineToString(end.Y, true, 0, end.X);
                    if (bufferLine is { IsWrapped: true })
                    {
                        result[^1] += lineText;
                    }
                    else
                    {
                        result.Add(lineText);
                    }
                }
            }

            // Format string by replacing non-breaking space chars with regular spaces and joining the array into a
            // multi-line string.
            var builder = new StringBuilder();
            var separator = _isWindows ? "\r\n" : "\n";
            for (var i = 0; i < result.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(separator);
                }

                builder.Append(result[i].Replace(NonBreakingSpace, ' '));
            }

            return builder.ToString();
        }
    }

    /// <summary>Clears the selection.</summary>
    internal void Reset() => ClearSelection();

    /// <summary>Disables the selection manager; useful when terminal mouse events are enabled.</summary>
    internal void Disable()
    {
        ClearSelection();
        _enabled = false;
    }

    /// <summary>Enables the selection manager.</summary>
    internal void Enable() => _enabled = true;

    /// <summary>Clears the current terminal selection.</summary>
    internal void ClearSelection()
    {
        Model.ClearSelection();
        SelectionChanged?.Invoke();
    }

    /// <summary>Whether the cell is part of the selection.</summary>
    /// <param name="x">Column.</param>
    /// <param name="y">Line of the buffer.</param>
    /// <returns><see langword="true"/> when selected.</returns>
    internal bool IsCellInSelection(int x, int y) =>
        Model.FinalSelectionStart is { } start && Model.FinalSelectionEnd is { } end && AreCoordsInSelection((x, y), start, end);

    /// <summary>Whether <paramref name="coords"/> lie between <paramref name="start"/> (inclusive) and <paramref name="end"/>.</summary>
    /// <param name="coords">The position to test.</param>
    /// <param name="start">Start of the selection.</param>
    /// <param name="end">End of the selection.</param>
    /// <returns><see langword="true"/> when inside.</returns>
    internal static bool AreCoordsInSelection((int X, int Y) coords, (int X, int Y) start, (int X, int Y) end) =>
        (coords.Y > start.Y && coords.Y < end.Y)
        || (start.Y == end.Y && coords.Y == start.Y && coords.X >= start.X && coords.X < end.X)
        || (start.Y < end.Y && coords.Y == end.Y && coords.X < end.X)
        || (start.Y < end.Y && coords.Y == start.Y && coords.X >= start.X);

    /// <summary>Selects all text within the terminal.</summary>
    internal void SelectAll()
    {
        Model.IsSelectAllActive = true;
        SelectionChanged?.Invoke();
    }

    /// <summary>Selects the lines from <paramref name="start"/> to <paramref name="end"/> (inclusive, clamped to the buffer).</summary>
    /// <param name="start">First line.</param>
    /// <param name="end">Last line.</param>
    internal void SelectLines(int start, int end)
    {
        Model.ClearSelection();
        start = Math.Max(start, 0);
        end = Math.Min(end, _bufferService.Buffer.Lines.Length - 1);
        Model.SelectionStart = (0, start);
        Model.SelectionEnd = (_bufferService.Cols, end);
        SelectionChanged?.Invoke();
    }

    /// <summary>Sets a selection that starts at <paramref name="col"/>, <paramref name="row"/> and spans <paramref name="length"/> cells.</summary>
    /// <param name="col">Start column.</param>
    /// <param name="row">Start line of the buffer.</param>
    /// <param name="length">Length in cells.</param>
    internal void SetSelection(int col, int row, int length)
    {
        Model.ClearSelection();
        Model.SelectionStart = (col, row);
        Model.SelectionStartLength = length;
        SelectionChanged?.Invoke();
    }

    /// <summary>Whether the pointer event forces selection even when the terminal is in mouse events mode.</summary>
    /// <param name="ev">The pointer event.</param>
    /// <returns><see langword="true"/> when selection is forced (Shift; on macOS Alt with the option, which is off here).</returns>
    internal bool ShouldForceSelection(TerminalPointerEvent ev) => !_isMac && ev.Shift;

    /// <summary>Whether the selection should operate in column select mode.</summary>
    /// <param name="ev">The pointer event.</param>
    /// <returns><see langword="true"/> when Alt is held.</returns>
    internal static bool ShouldColumnSelect(TerminalPointerEvent ev) => ev.Alt;

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }

        _trimListener.Dispose();
    }

    private void HandleTrim(int amount)
    {
        if (Model.HandleTrim(amount))
        {
            SelectionChanged?.Invoke();
        }
    }

    private void HandleBufferActivate(BufferActivateEvent e)
    {
        ClearSelection();

        // Only adjust the selection on trim, shiftElements is rarely used (only in reverseIndex) and delete in a
        // splice is only ever used when the same number of elements was just added.
        _trimListener.Dispose();
        _trimListener = e.ActiveBuffer.Lines.OnTrim.Subscribe(HandleTrim);
    }
}
