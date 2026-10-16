// Inspired by xtermjs/xterm.js: src/vs/base/common/event.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Core.Common;

/// <summary>An event that listeners can subscribe to.</summary>
/// <typeparam name="T">The payload type.</typeparam>
internal interface IEvent<out T>
{
    /// <summary>Adds a listener. Dispose the result to remove it.</summary>
    IDisposable Subscribe(Action<T> listener);
}

/// <summary>Raises an event to its listeners, in subscription order.</summary>
/// <typeparam name="T">The payload type.</typeparam>
/// <remarks>Threading: not thread-safe; used from the parser thread only (ADR 0002).</remarks>
internal sealed class Emitter<T> : IDisposable
{
    private Action<T>[] _listeners = [];
    private bool _isDisposed;

    /// <summary>The event to expose to subscribers.</summary>
    public IEvent<T> Event => new EventView(this);

    /// <summary>Calls the listeners subscribed before this call. Does nothing once disposed.</summary>
    public void Fire(T value)
    {
        if (_isDisposed)
        {
            return;
        }

        Action<T>[] snapshot = _listeners;
        foreach (Action<T> listener in snapshot)
        {
            // A listener removed by an earlier listener of this same Fire must not be called.
            if (Array.IndexOf(_listeners, listener) >= 0)
            {
                listener(value);
            }
        }
    }

    /// <summary>Removes every listener.</summary>
    public void Dispose()
    {
        _isDisposed = true;
        _listeners = [];
    }

    private IDisposable Subscribe(Action<T> listener)
    {
        if (_isDisposed)
        {
            return Disposables.Empty;
        }

        // Each subscription is a distinct wrapper, so the same delegate can be subscribed twice.
        Action<T> entry = value => listener(value);
        _listeners = [.. _listeners, entry];
        return Disposables.Create(() => _listeners = Array.FindAll(_listeners, l => !ReferenceEquals(l, entry)));
    }

    private sealed class EventView(Emitter<T> owner) : IEvent<T>
    {
        public IDisposable Subscribe(Action<T> listener) => owner.Subscribe(listener);
    }
}
