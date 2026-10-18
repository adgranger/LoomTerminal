// Ported from xtermjs/xterm.js: src/common/buffer/BufferSet.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Common;
using LoomTerminal.Core.Services;

namespace LoomTerminal.Core.Buffer;

/// <summary>The payload of <see cref="BufferSet.OnBufferActivate"/>.</summary>
/// <param name="ActiveBuffer">The buffer that is now active.</param>
/// <param name="InactiveBuffer">The other buffer.</param>
internal readonly record struct BufferActivateEvent(Buffer ActiveBuffer, Buffer InactiveBuffer);

/// <summary>
/// The set of the two buffers used by terminals (normal and alt), with utilities for working with them.
/// </summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal sealed class BufferSet : Disposable
{
    private readonly IOptionsService _optionsService;
    private readonly IBufferService _bufferService;
    private readonly Emitter<BufferActivateEvent> _onBufferActivate;
    private Buffer _normal;
    private Buffer _alt;
    private Buffer _activeBuffer;

    /// <summary>Creates a buffer set for the given terminal.</summary>
    public BufferSet(IOptionsService optionsService, IBufferService bufferService)
    {
        _optionsService = optionsService;
        _bufferService = bufferService;
        _onBufferActivate = Register(new Emitter<BufferActivateEvent>());
        (_normal, _alt) = CreateBuffers();
        _activeBuffer = _normal;
        AnnounceActivation(_normal, _alt);
        SetupTabStops();
        Register(_optionsService.OnSpecificOptionChange(TerminalOption.Scrollback, () => Resize(_bufferService.Cols, _bufferService.Rows)));
        Register(_optionsService.OnSpecificOptionChange(TerminalOption.TabStopWidth, () => SetupTabStops()));
    }

    /// <summary>Raised when the active buffer changes.</summary>
    public IEvent<BufferActivateEvent> OnBufferActivate => _onBufferActivate.Event;

    /// <summary>The alt buffer.</summary>
    public Buffer Alt => _alt;

    /// <summary>The currently active buffer.</summary>
    public Buffer Active => _activeBuffer;

    /// <summary>The normal buffer.</summary>
    public Buffer Normal => _normal;

    /// <summary>Recreates both buffers; the normal one becomes active.</summary>
    public void Reset()
    {
        (_normal, _alt) = CreateBuffers();
        _activeBuffer = _normal;
        AnnounceActivation(_normal, _alt);

        SetupTabStops();
    }

    /// <summary>Sets the normal buffer as the active one.</summary>
    public void ActivateNormalBuffer()
    {
        if (_activeBuffer == _normal)
        {
            return;
        }

        _normal.X = _alt.X;
        _normal.Y = _alt.Y;

        // The alt buffer should always be cleared when we switch to the normal buffer. This frees up memory since the
        // alt buffer should always be new when activated.
        _alt.ClearAllMarkers();
        _alt.Clear();
        _activeBuffer = _normal;
        AnnounceActivation(_normal, _alt);
    }

    /// <summary>Sets the alt buffer as the active one.</summary>
    public void ActivateAltBuffer(AttributeData? fillAttr = null)
    {
        if (_activeBuffer == _alt)
        {
            return;
        }

        // Since the alt buffer is always cleared when the normal buffer is activated, we want to fill it when
        // switching to it.
        _alt.FillViewportRows(fillAttr);
        _alt.X = _normal.X;
        _alt.Y = _normal.Y;
        _activeBuffer = _alt;
        AnnounceActivation(_alt, _normal);
    }

    /// <summary>Resizes both the normal and the alt buffers, adjusting their data accordingly.</summary>
    public void Resize(int newCols, int newRows)
    {
        _normal.Resize(newCols, newRows);
        _alt.Resize(newCols, newRows);
        SetupTabStops(newCols);
    }

    /// <summary>Sets up the tab stops, starting from <paramref name="i"/> (or from scratch).</summary>
    public void SetupTabStops(int? i = null)
    {
        _normal.SetupTabStops(i);
        _alt.SetupTabStops(i);
    }

    private (Buffer Normal, Buffer Alt) CreateBuffers()
    {
        Buffer normal = new(true, _optionsService, _bufferService);
        normal.FillViewportRows();

        // The alt buffer should never have scrollback.
        // See http://invisible-island.net/xterm/ctlseqs/ctlseqs.html#h2-The-Alternate-Screen-Buffer
        Buffer alt = new(false, _optionsService, _bufferService);
        return (normal, alt);
    }

    private void AnnounceActivation(Buffer active, Buffer inactive) =>
        _onBufferActivate.Fire(new BufferActivateEvent(active, inactive));
}
