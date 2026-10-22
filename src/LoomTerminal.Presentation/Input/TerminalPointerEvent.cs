namespace LoomTerminal.Presentation.Input;

/// <summary>The button of a <see cref="TerminalPointerEvent"/>.</summary>
public enum PointerButton
{
    /// <summary>No button (a move without a button).</summary>
    None,

    /// <summary>The left button.</summary>
    Left,

    /// <summary>The middle button.</summary>
    Middle,

    /// <summary>The right button.</summary>
    Right,

    /// <summary>The wheel, scrolled up.</summary>
    WheelUp,

    /// <summary>The wheel, scrolled down.</summary>
    WheelDown,
}

/// <summary>What a <see cref="TerminalPointerEvent"/> reports.</summary>
public enum PointerAction
{
    /// <summary>A button was pressed.</summary>
    Down,

    /// <summary>A button was released.</summary>
    Up,

    /// <summary>The pointer moved.</summary>
    Move,

    /// <summary>The wheel turned.</summary>
    Wheel,
}

/// <summary>The unit of <see cref="TerminalPointerEvent.WheelDeltaY"/> (the DOM values).</summary>
public enum WheelDeltaMode
{
    /// <summary>The delta is in pixels.</summary>
    Pixel,

    /// <summary>The delta is in lines.</summary>
    Line,

    /// <summary>The delta is in pages.</summary>
    Page,
}

/// <summary>Backend-neutral pointer event, in cell and pixel coordinates of the terminal area.</summary>
/// <param name="Column">Column under the pointer (zero based).</param>
/// <param name="Row">Row under the pointer (zero based, relative to the viewport).</param>
/// <param name="X">Horizontal position in pixels, relative to the terminal area.</param>
/// <param name="Y">Vertical position in pixels, relative to the terminal area.</param>
/// <param name="Button">The button concerned.</param>
/// <param name="Action">What happened.</param>
/// <param name="Ctrl">Whether Ctrl is held.</param>
/// <param name="Alt">Whether Alt is held.</param>
/// <param name="Shift">Whether Shift is held.</param>
/// <remarks>Threading: immutable; any thread may use it.</remarks>
public sealed record TerminalPointerEvent(
    int Column,
    int Row,
    int X,
    int Y,
    PointerButton Button,
    PointerAction Action,
    bool Ctrl,
    bool Alt,
    bool Shift)
{
    /// <summary>Vertical wheel delta; negative scrolls up (DOM <c>deltaY</c>).</summary>
    public double WheelDeltaY { get; init; }

    /// <summary>The unit of <see cref="WheelDeltaY"/>.</summary>
    public WheelDeltaMode DeltaMode { get; init; } = WheelDeltaMode.Pixel;

    /// <summary>Number of consecutive clicks (1 for a single click, 2 for a double click…).</summary>
    public int ClickCount { get; init; } = 1;
}
