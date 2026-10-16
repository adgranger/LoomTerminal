// Inspired by xtermjs/xterm.js: src/vs/base/common/lifecycle.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Core.Common;

/// <summary>Base class of objects that own other disposable objects.</summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal abstract class Disposable : IDisposable
{
    private readonly List<IDisposable> _registered = [];

    /// <summary>True once <see cref="Dispose"/> has been called.</summary>
    public bool IsDisposed { get; private set; }

    /// <summary>Disposes the registered objects (in reverse order), then calls <see cref="DisposeCore"/>. Idempotent.</summary>
    public void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        IsDisposed = true;
        for (int i = _registered.Count - 1; i >= 0; i--)
        {
            _registered[i].Dispose();
        }

        _registered.Clear();
        DisposeCore();
    }

    /// <summary>Registers an object to be disposed with this one. If already disposed, the object is disposed at once.</summary>
    protected T Register<T>(T disposable) where T : IDisposable
    {
        if (IsDisposed)
        {
            disposable.Dispose();
        }
        else
        {
            _registered.Add(disposable);
        }

        return disposable;
    }

    /// <summary>Called once by <see cref="Dispose"/>, after the registered objects are disposed.</summary>
    protected virtual void DisposeCore()
    {
    }
}

/// <summary>Factories of simple disposables (<c>toDisposable</c> in xterm.js).</summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal static class Disposables
{
    /// <summary>A disposable that does nothing.</summary>
    public static IDisposable Empty { get; } = new ActionDisposable(null);

    /// <summary>A disposable that runs <paramref name="onDispose"/> the first time it is disposed.</summary>
    public static IDisposable Create(Action onDispose) => new ActionDisposable(onDispose);

    private sealed class ActionDisposable(Action? action) : IDisposable
    {
        private Action? _action = action;

        public void Dispose()
        {
            Action? toRun = _action;
            _action = null;
            toRun?.Invoke();
        }
    }
}

/// <summary>A set of disposables disposed together.</summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal sealed class DisposableStore : IDisposable
{
    private readonly List<IDisposable> _items = [];

    /// <summary>Adds an object; it is disposed with the store (at once if the store is already disposed).</summary>
    public T Add<T>(T disposable) where T : IDisposable
    {
        if (_isDisposed)
        {
            disposable.Dispose();
        }
        else
        {
            _items.Add(disposable);
        }

        return disposable;
    }

    private bool _isDisposed;

    /// <summary>Disposes every object and empties the store.</summary>
    public void Clear()
    {
        IDisposable[] items = [.. _items];
        _items.Clear();
        foreach (IDisposable item in items)
        {
            item.Dispose();
        }
    }

    /// <summary>Clears the store; later additions are disposed immediately.</summary>
    public void Dispose()
    {
        _isDisposed = true;
        Clear();
    }
}

/// <summary>Holds one disposable; assigning a new value disposes the previous one.</summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal sealed class MutableDisposable<T> : IDisposable where T : class, IDisposable
{
    private T? _value;

    /// <summary>The current value. Setting a different value disposes the previous one.</summary>
    public T? Value
    {
        get => _value;
        set
        {
            if (ReferenceEquals(_value, value))
            {
                return;
            }

            T? previous = _value;
            _value = value;
            previous?.Dispose();
        }
    }

    /// <summary>Disposes the value and sets it to null.</summary>
    public void Clear() => Value = null;

    /// <summary>Disposes the value.</summary>
    public void Dispose() => Clear();
}
