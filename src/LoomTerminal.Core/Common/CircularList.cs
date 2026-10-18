// Ported from xtermjs/xterm.js: src/common/CircularList.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Core.Common;

/// <summary>Describes items inserted into a <see cref="CircularList{T}"/>.</summary>
/// <param name="Index">The index of the first inserted item.</param>
/// <param name="Amount">The number of inserted items.</param>
internal readonly record struct InsertEvent(int Index, int Amount);

/// <summary>Describes items deleted from a <see cref="CircularList{T}"/>.</summary>
/// <param name="Index">The index of the first deleted item.</param>
/// <param name="Amount">The number of deleted items.</param>
internal readonly record struct DeleteEvent(int Index, int Amount);

/// <summary>
/// Represents a circular list; a list with a maximum size that wraps around when <see cref="Push"/> is called,
/// overriding values at the start of the list.
/// </summary>
/// <typeparam name="T">The element type (a reference type; <c>undefined</c> in xterm.js is <c>null</c>).</typeparam>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal sealed class CircularList<T> : Disposable where T : class
{
    private readonly Emitter<DeleteEvent> _onDeleteEmitter;
    private readonly Emitter<InsertEvent> _onInsertEmitter;
    private readonly Emitter<int> _onTrimEmitter;
    private T?[] _array;
    private int _maxLength;
    private int _startIndex;
    private int _length;

    /// <summary>Creates an empty list.</summary>
    /// <param name="maxLength">The maximum number of items.</param>
    public CircularList(int maxLength)
    {
        _onDeleteEmitter = Register(new Emitter<DeleteEvent>());
        _onInsertEmitter = Register(new Emitter<InsertEvent>());
        _onTrimEmitter = Register(new Emitter<int>());
        _maxLength = maxLength;
        _array = new T?[maxLength];
        _startIndex = 0;
        _length = 0;
    }

    /// <summary>The emitter behind <see cref="OnDelete"/>; <c>BufferReflow</c> fires it to tell markers about removed lines.</summary>
    internal Emitter<DeleteEvent> OnDeleteEmitter => _onDeleteEmitter;

    /// <summary>The emitter behind <see cref="OnTrim"/>; tests fire it to simulate a trimmed line.</summary>
    internal Emitter<int> OnTrimEmitter => _onTrimEmitter;

    /// <summary>Raised after items were deleted by <see cref="Splice"/>.</summary>
    public IEvent<DeleteEvent> OnDelete => _onDeleteEmitter.Event;

    /// <summary>Raised after items were inserted by <see cref="Splice"/>.</summary>
    public IEvent<InsertEvent> OnInsert => _onInsertEmitter.Event;

    /// <summary>Raised with the number of items dropped from the start of the list.</summary>
    public IEvent<int> OnTrim => _onTrimEmitter.Event;

    /// <summary>The maximum number of items. Changing it reallocates the backing array.</summary>
    public int MaxLength
    {
        get => _maxLength;
        set
        {
            // There was no change in maxLength, return early.
            if (_maxLength == value)
            {
                return;
            }

            // Reconstruct array, starting at index 0. Only transfer values from the indexes 0 to length.
            T?[] newArray = new T?[value];
            for (int i = 0; i < Math.Min(value, Length); i++)
            {
                newArray[i] = _array[GetCyclicIndex(i)];
            }

            _array = newArray;
            _maxLength = value;
            _startIndex = 0;
        }
    }

    /// <summary>The number of items. Growing it exposes <c>null</c> slots; shrinking only moves the length.</summary>
    public int Length
    {
        get => _length;
        set
        {
            if (value > _length)
            {
                for (int i = _length; i < value; i++)
                {
                    _array[i] = null;
                }
            }

            _length = value;
        }
    }

    /// <summary>True when the list is at its maximum length.</summary>
    public bool IsFull => _length == _maxLength;

    /// <summary>Gets the value at an index. There is no bounds check: the index is circular.</summary>
    /// <param name="index">The index of the value to get.</param>
    public T? Get(int index) => _array[GetCyclicIndex(index)];

    /// <summary>Sets the value at an index. There is no bounds check: the index is circular.</summary>
    /// <param name="index">The index to set.</param>
    /// <param name="value">The value to set.</param>
    public void Set(int index, T? value) => _array[GetCyclicIndex(index)] = value;

    /// <summary>Pushes a value, wrapping around and overriding index 0 if the maximum length is reached.</summary>
    /// <param name="value">The value to push onto the list.</param>
    public void Push(T value)
    {
        _array[GetCyclicIndex(_length)] = value;
        if (_length == _maxLength)
        {
            _startIndex = ++_startIndex % _maxLength;
            _onTrimEmitter.Fire(1);
        }
        else
        {
            _length++;
        }
    }

    /// <summary>Advances the ring buffer index and returns the current element for recycling. The list must be full.</summary>
    /// <exception cref="InvalidOperationException">The list is not full.</exception>
    public T Recycle()
    {
        if (_length != _maxLength)
        {
            throw new InvalidOperationException("Can only recycle when the buffer is full");
        }

        _startIndex = ++_startIndex % _maxLength;
        _onTrimEmitter.Fire(1);
        return _array[GetCyclicIndex(_length - 1)]!;
    }

    /// <summary>Removes and returns the last value on the list.</summary>
    public T? Pop() => _array[GetCyclicIndex(_length-- - 1)];

    /// <summary>
    /// Deletes and/or inserts items at a particular index (in that order). This may shift all values in the list in
    /// the worst case.
    /// </summary>
    /// <param name="start">The index to delete and/or insert.</param>
    /// <param name="deleteCount">The number of elements to delete.</param>
    /// <param name="items">The items to insert.</param>
    public void Splice(int start, int deleteCount, params ReadOnlySpan<T> items)
    {
        // Delete items
        if (deleteCount != 0)
        {
            for (int i = start; i < _length - deleteCount; i++)
            {
                _array[GetCyclicIndex(i)] = _array[GetCyclicIndex(i + deleteCount)];
            }

            _length -= deleteCount;
            _onDeleteEmitter.Fire(new DeleteEvent(start, deleteCount));
        }

        // Add items
        for (int i = _length - 1; i >= start; i--)
        {
            _array[GetCyclicIndex(i + items.Length)] = _array[GetCyclicIndex(i)];
        }

        for (int i = 0; i < items.Length; i++)
        {
            _array[GetCyclicIndex(start + i)] = items[i];
        }

        if (items.Length != 0)
        {
            _onInsertEmitter.Fire(new InsertEvent(start, items.Length));
        }

        // Adjust length as needed
        if (_length + items.Length > _maxLength)
        {
            int countToTrim = _length + items.Length - _maxLength;
            _startIndex += countToTrim;
            _length = _maxLength;
            _onTrimEmitter.Fire(countToTrim);
        }
        else
        {
            _length += items.Length;
        }
    }

    /// <summary>Trims a number of items from the start of the list.</summary>
    /// <param name="count">The number of items to remove.</param>
    public void TrimStart(int count)
    {
        if (count > _length)
        {
            count = _length;
        }

        _startIndex += count;
        _length -= count;
        _onTrimEmitter.Fire(count);
    }

    /// <summary>Copies <paramref name="count"/> items starting at <paramref name="start"/> by <paramref name="offset"/> places.</summary>
    /// <param name="start">The index of the first item to move.</param>
    /// <param name="count">The number of items to move.</param>
    /// <param name="offset">The distance to move them (negative moves backwards).</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="start"/> is out of range, or the shift goes below index 0.</exception>
    public void ShiftElements(int start, int count, int offset)
    {
        if (count <= 0)
        {
            return;
        }

        if (start < 0 || start >= _length)
        {
            throw new ArgumentOutOfRangeException(nameof(start), "start argument out of range");
        }

        if (start + offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Cannot shift elements in list beyond index 0");
        }

        if (offset > 0)
        {
            for (int i = count - 1; i >= 0; i--)
            {
                Set(start + i + offset, Get(start + i));
            }

            int expandListBy = start + count + offset - _length;
            if (expandListBy > 0)
            {
                _length += expandListBy;
                while (_length > _maxLength)
                {
                    _length--;
                    _startIndex++;
                    _onTrimEmitter.Fire(1);
                }
            }
        }
        else
        {
            for (int i = 0; i < count; i++)
            {
                Set(start + i + offset, Get(start + i));
            }
        }
    }

    // Gets the index in the backing array for a regular index.
    private int GetCyclicIndex(int index) => (_startIndex + index) % _maxLength;
}
