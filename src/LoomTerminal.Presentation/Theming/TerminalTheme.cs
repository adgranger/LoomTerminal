namespace LoomTerminal.Presentation.Theming;

/// <summary>
/// Colors of a terminal, as CSS strings (<c>"#1e1e1e"</c>, <c>"#1e1e1e80"</c>, <c>"rgb(30, 30, 30)"</c>), like the
/// xterm.js <c>ITheme</c>. A <see langword="null"/> or invalid color means "use the default".
/// </summary>
/// <remarks>Threading: immutable; any thread may use it.</remarks>
public sealed record TerminalTheme
{
    /// <summary>Default text color.</summary>
    public string? Foreground { get; init; }

    /// <summary>Default background color.</summary>
    public string? Background { get; init; }

    /// <summary>Cursor color.</summary>
    public string? Cursor { get; init; }

    /// <summary>Color of the character under a block cursor.</summary>
    public string? CursorAccent { get; init; }

    /// <summary>Background of the selection; use an alpha channel to keep the text readable.</summary>
    public string? SelectionBackground { get; init; }

    /// <summary>Text color inside the selection; <see langword="null"/> keeps the text colors.</summary>
    public string? SelectionForeground { get; init; }

    /// <summary>Background of the selection when the terminal is not focused.</summary>
    public string? SelectionInactiveBackground { get; init; }

    /// <summary>ANSI color 0.</summary>
    public string? Black { get; init; }

    /// <summary>ANSI color 1.</summary>
    public string? Red { get; init; }

    /// <summary>ANSI color 2.</summary>
    public string? Green { get; init; }

    /// <summary>ANSI color 3.</summary>
    public string? Yellow { get; init; }

    /// <summary>ANSI color 4.</summary>
    public string? Blue { get; init; }

    /// <summary>ANSI color 5.</summary>
    public string? Magenta { get; init; }

    /// <summary>ANSI color 6.</summary>
    public string? Cyan { get; init; }

    /// <summary>ANSI color 7.</summary>
    public string? White { get; init; }

    /// <summary>ANSI color 8.</summary>
    public string? BrightBlack { get; init; }

    /// <summary>ANSI color 9.</summary>
    public string? BrightRed { get; init; }

    /// <summary>ANSI color 10.</summary>
    public string? BrightGreen { get; init; }

    /// <summary>ANSI color 11.</summary>
    public string? BrightYellow { get; init; }

    /// <summary>ANSI color 12.</summary>
    public string? BrightBlue { get; init; }

    /// <summary>ANSI color 13.</summary>
    public string? BrightMagenta { get; init; }

    /// <summary>ANSI color 14.</summary>
    public string? BrightCyan { get; init; }

    /// <summary>ANSI color 15.</summary>
    public string? BrightWhite { get; init; }

    /// <summary>ANSI colors 16 to 255; missing or invalid entries use the default.</summary>
    public IReadOnlyList<string>? ExtendedAnsi { get; init; }
}
