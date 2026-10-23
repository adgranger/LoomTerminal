using LoomTerminal.Presentation.Session;
using LoomTerminal.Presentation.Theming;
using Microsoft.UI.Xaml;

namespace LoomTerminal.WinUI.Controls;

public sealed partial class TerminalControl
{
    /// <summary>Identifies <see cref="LineHeight"/>.</summary>
    public static readonly DependencyProperty LineHeightProperty = DependencyProperty.Register(
        nameof(LineHeight), typeof(double), typeof(TerminalControl), new PropertyMetadata(1.15));

    /// <summary>Identifies <see cref="CursorStyle"/>.</summary>
    public static readonly DependencyProperty CursorStyleProperty = DependencyProperty.Register(
        nameof(CursorStyle), typeof(TerminalCursorStyle), typeof(TerminalControl), new PropertyMetadata(TerminalCursorStyle.Block));

    /// <summary>Identifies <see cref="CursorBlink"/>.</summary>
    public static readonly DependencyProperty CursorBlinkProperty = DependencyProperty.Register(
        nameof(CursorBlink), typeof(bool), typeof(TerminalControl), new PropertyMetadata(false));

    /// <summary>The dark, VS Code-like theme used until the host sets <see cref="Theme"/>.</summary>
    private static readonly TerminalTheme DefaultTheme = new()
    {
        Foreground = "#cccccc",
        Background = "#1e1e1e",
        Cursor = "#aeafad",
        CursorAccent = "#1e1e1e",
        SelectionBackground = "#264f78",
        SelectionInactiveBackground = "#3a3d41",
        Black = "#000000",
        Red = "#cd3131",
        Green = "#0dbc79",
        Yellow = "#e5e510",
        Blue = "#2472c8",
        Magenta = "#bc3fbc",
        Cyan = "#11a8cd",
        White = "#e5e5e5",
        BrightBlack = "#666666",
        BrightRed = "#f14c4c",
        BrightGreen = "#23d18b",
        BrightYellow = "#f5f543",
        BrightBlue = "#3b8eea",
        BrightMagenta = "#d670d6",
        BrightCyan = "#29b8db",
        BrightWhite = "#e5e5e5",
    };

    // Declared after DefaultTheme: static initializers run in order, the default must not be null.
    /// <summary>Identifies <see cref="Theme"/>.</summary>
    public static readonly DependencyProperty ThemeProperty = DependencyProperty.Register(
        nameof(Theme), typeof(TerminalTheme), typeof(TerminalControl), new PropertyMetadata(DefaultTheme));

    /// <summary>The colors of the terminal.</summary>
    public TerminalTheme Theme
    {
        get => (TerminalTheme)GetValue(ThemeProperty);
        set => SetValue(ThemeProperty, value);
    }

    /// <summary>Line height as a multiple of the font size (default 1.15).</summary>
    public double LineHeight
    {
        get => (double)GetValue(LineHeightProperty);
        set => SetValue(LineHeightProperty, value);
    }

    /// <summary>Shape of the cursor.</summary>
    public TerminalCursorStyle CursorStyle
    {
        get => (TerminalCursorStyle)GetValue(CursorStyleProperty);
        set => SetValue(CursorStyleProperty, value);
    }

    /// <summary>Whether the cursor blinks.</summary>
    public bool CursorBlink
    {
        get => (bool)GetValue(CursorBlinkProperty);
        set => SetValue(CursorBlinkProperty, value);
    }
}
