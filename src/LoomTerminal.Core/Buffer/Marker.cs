// Ported from xtermjs/xterm.js: src/common/buffer/Marker.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Common;

namespace LoomTerminal.Core.Buffer;

/// <summary>A marker: a line of the buffer that follows the content as it scrolls.</summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal sealed class Marker : IMarker
{
    private static int _nextId = 1;

    private readonly List<IDisposable> _disposables = [];
    private readonly Emitter<EventArgs> _onDispose;

    /// <summary>Creates a marker on <paramref name="line"/>.</summary>
    public Marker(int line)
    {
        Line = line;
        Id = _nextId++;
        _onDispose = Register(new Emitter<EventArgs>());
    }

    /// <inheritdoc />
    public int Id { get; }

    /// <inheritdoc />
    public bool IsDisposed { get; private set; }

    /// <summary>The line of the marker, <c>-1</c> once disposed. The buffer moves it as lines are trimmed or inserted.</summary>
    public int Line { get; set; }

    /// <inheritdoc />
    public IEvent<EventArgs> OnDispose => _onDispose.Event;

    /// <summary>Disposes the marker: sets <see cref="Line"/> to -1, raises <see cref="OnDispose"/> and disposes the registered objects.</summary>
    public void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        IsDisposed = true;
        Line = -1;

        // Emit before disposing such that dispose listeners get a chance to react
        _onDispose.Fire(EventArgs.Empty);
        foreach (IDisposable disposable in _disposables)
        {
            disposable.Dispose();
        }

        _disposables.Clear();
    }

    /// <summary>Registers an object to dispose together with the marker.</summary>
    public T Register<T>(T disposable)
        where T : IDisposable
    {
        _disposables.Add(disposable);
        return disposable;
    }
}
