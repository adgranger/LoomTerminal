// Ported from xtermjs/xterm.js: src/common/buffer/Buffer.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Services;

namespace LoomTerminal.Core.Buffer;

internal sealed partial class Buffer
{
    private int _memoryCleanupPosition;

    /// <summary>
    /// Whether a memory cleanup is pending after a resize. The session calls <see cref="BatchedMemoryCleanup"/> when the
    /// parser thread is idle (xterm.js schedules it with an <c>IdleTaskQueue</c>).
    /// </summary>
    public bool MemoryCleanupPending { get; private set; }

    // Windows pty backends reprint the screen with their own view of the world.
    private bool IsReflowEnabled
    {
        get
        {
            WindowsPtyOptions? windowsPty = _optionsService.RawOptions.WindowsPty;
            if (windowsPty is { BuildNumber: > 0 })
            {
                return _hasScrollback && windowsPty.Backend == "conpty" && windowsPty.BuildNumber >= 21376;
            }

            return _hasScrollback && !_optionsService.RawOptions.WindowsMode;
        }
    }

    /// <summary>Resizes the buffer, adjusting its data accordingly.</summary>
    /// <param name="newCols">The new number of columns.</param>
    /// <param name="newRows">The new number of rows.</param>
    public void Resize(int newCols, int newRows)
    {
        // store reference to null cell with default attrs
        CellData nullCell = GetNullCell(AttributeData.DefaultAttrData);

        // count bufferlines with overly big memory to be cleaned afterwards
        int dirtyMemoryLines = 0;

        // Increase max length if needed before adjustments to allow space to fill as required.
        int newMaxLength = GetCorrectBufferLength(newRows);
        if (newMaxLength > Lines.MaxLength)
        {
            Lines.MaxLength = newMaxLength;
        }

        // The following adjustments should only happen if the buffer has been initialized/filled.
        if (Lines.Length > 0)
        {
            // Deal with columns increasing (reducing needs to happen after reflow)
            if (_cols < newCols)
            {
                for (int i = 0; i < Lines.Length; i++)
                {
                    dirtyMemoryLines += Lines.Get(i)!.Resize(newCols, nullCell) ? 1 : 0;
                }
            }

            // Resize rows in both directions as needed
            int addToY = 0;
            if (_rows < newRows)
            {
                for (int y = _rows; y < newRows; y++)
                {
                    if (Lines.Length < newRows + YBase)
                    {
                        WindowsPtyOptions? windowsPty = _optionsService.RawOptions.WindowsPty;
                        if (_optionsService.RawOptions.WindowsMode || windowsPty?.Backend is not null || windowsPty?.BuildNumber is not null)
                        {
                            // Just add the new missing rows on Windows as conpty reprints the screen with it's view of
                            // the world. Once a line enters scrollback for conpty it remains there
                            Lines.Push(new BufferLine(newCols, nullCell));
                        }
                        else if (YBase > 0 && Lines.Length <= YBase + Y + addToY + 1)
                        {
                            // There is room above the buffer and there are no empty elements below the line, scroll up
                            YBase--;
                            addToY++;
                            if (YDisp > 0)
                            {
                                // Viewport is at the top of the buffer, must increase downwards
                                YDisp--;
                            }
                        }
                        else
                        {
                            // Add a blank line if there is no buffer left at the top to scroll to, or if there are
                            // blank lines after the cursor
                            Lines.Push(new BufferLine(newCols, nullCell));
                        }
                    }
                }
            }
            else
            {
                // (_rows >= newRows)
                for (int y = _rows; y > newRows; y--)
                {
                    if (Lines.Length > newRows + YBase)
                    {
                        if (Lines.Length > YBase + Y + 1)
                        {
                            // The line is a blank line below the cursor, remove it
                            Lines.Pop();
                        }
                        else
                        {
                            // The line is the cursor, scroll down
                            YBase++;
                            YDisp++;
                        }
                    }
                }
            }

            // Reduce max length if needed after adjustments, this is done after as it would otherwise cut data from
            // the bottom of the buffer.
            if (newMaxLength < Lines.MaxLength)
            {
                // Trim from the top of the buffer and adjust YBase and YDisp.
                int amountToTrim = Lines.Length - newMaxLength;
                if (amountToTrim > 0)
                {
                    Lines.TrimStart(amountToTrim);
                    YBase = Math.Max(YBase - amountToTrim, 0);
                    YDisp = Math.Max(YDisp - amountToTrim, 0);
                    SavedY = Math.Max(SavedY - amountToTrim, 0);
                }

                Lines.MaxLength = newMaxLength;
            }

            // Make sure that the cursor stays on screen
            X = Math.Min(X, newCols - 1);
            Y = Math.Min(Y, newRows - 1);
            if (addToY != 0)
            {
                Y += addToY;
            }

            SavedX = Math.Min(SavedX, newCols - 1);

            ScrollTop = 0;
        }

        ScrollBottom = newRows - 1;

        if (IsReflowEnabled)
        {
            Reflow(newCols, newRows);

            // Trim the end of the line off if cols shrunk
            if (_cols > newCols)
            {
                for (int i = 0; i < Lines.Length; i++)
                {
                    dirtyMemoryLines += Lines.Get(i)!.Resize(newCols, nullCell) ? 1 : 0;
                }
            }
        }

        _cols = newCols;
        _rows = newRows;

        MemoryCleanupPending = false;

        // schedule memory cleanup only, if more than 10% of the lines are affected
        if (dirtyMemoryLines > 0.1 * Lines.Length)
        {
            _memoryCleanupPosition = 0;
            MemoryCleanupPending = true;
        }
    }

    /// <summary>
    /// Cleans up the memory of at most 100 lines that kept an oversized array after shrinking. Returns <c>true</c> if
    /// the cleanup has to run again (the JS task is re-queued then), <c>false</c> once it is finished.
    /// </summary>
    public bool BatchedMemoryCleanup()
    {
        bool normalRun = true;
        if (_memoryCleanupPosition >= Lines.Length)
        {
            // cleanup made it once through all lines, thus rescan in loop below to also catch shifted lines, which
            // should finish rather quick if there are no more cleanups pending
            _memoryCleanupPosition = 0;
            normalRun = false;
        }

        int counted = 0;
        while (_memoryCleanupPosition < Lines.Length)
        {
            counted += ((BufferLine)Lines.Get(_memoryCleanupPosition++)!).CleanupMemory();

            // cleanup max 100 lines per batch
            if (counted > 100)
            {
                return true;
            }
        }

        // normal runs always need another rescan afterwards; if we made it here with normalRun = false, we are in a
        // final run and can end the cleanup task for sure
        MemoryCleanupPending = normalRun;
        return normalRun;
    }
}
