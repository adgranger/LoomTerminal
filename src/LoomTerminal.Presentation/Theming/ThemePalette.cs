// Ported from xtermjs/xterm.js: src/browser/services/ThemeService.ts, src/browser/Types.ts (DEFAULT_ANSI_COLORS)
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Common;
using LoomTerminal.Core.InputHandling;
using LoomTerminal.Core.Input;

namespace LoomTerminal.Presentation.Theming;

/// <summary>
/// The resolved colors of a terminal: a <see cref="TerminalTheme"/> turned into the 256-color palette and the special
/// colors, plus the OSC overrides (set, restore, query) applied on top of it.
/// </summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal sealed class ThemePalette
{
    /// <summary>The 256 default ANSI colors: 16 named, the 6x6x6 cube, 24 grays.</summary>
    internal static readonly IReadOnlyList<TerminalColor> DefaultAnsiColors = CreateDefaultAnsiColors();

    private static readonly TerminalColor DefaultForeground = Css.ToColor("#ffffff");
    private static readonly TerminalColor DefaultBackground = Css.ToColor("#000000");
    private static readonly TerminalColor DefaultCursor = Css.ToColor("#ffffff");
    private static readonly TerminalColor DefaultCursorAccent = DefaultBackground;
    private static readonly TerminalColor DefaultSelection = new("rgba(255, 255, 255, 0.3)", 0xFFFFFF4D);

    private static readonly TerminalColor DefaultSearchMatch = Css.ToColor("#515c6a80");
    private static readonly TerminalColor DefaultSearchActiveMatch = Css.ToColor("#f8b64c80");

    private TerminalColor _restoreForeground;
    private TerminalColor _restoreBackground;
    private TerminalColor _restoreCursor;
    private TerminalColor[] _restoreAnsi = new TerminalColor[256];

    /// <summary>Creates the palette of <paramref name="theme"/> (the defaults when <see langword="null"/>).</summary>
    /// <param name="theme">The theme.</param>
    internal ThemePalette(TerminalTheme? theme = null)
    {
        SetTheme(theme);
    }

    /// <summary>Raised after any change of the colors.</summary>
    internal event Action? Changed;

    /// <summary>Default text color.</summary>
    internal TerminalColor Foreground { get; private set; }

    /// <summary>Default background color.</summary>
    internal TerminalColor Background { get; private set; }

    /// <summary>Cursor color, blended with the background.</summary>
    internal TerminalColor Cursor { get; private set; }

    /// <summary>Color of the character under a block cursor, blended with the background.</summary>
    internal TerminalColor CursorAccent { get; private set; }

    /// <summary>Text color inside the selection; <see langword="null"/> keeps the text colors.</summary>
    internal TerminalColor? SelectionForeground { get; private set; }

    /// <summary>Background of the search matches, with its alpha channel.</summary>
    internal TerminalColor SearchMatchBackground { get; private set; }

    /// <summary>Background of the active search match, with its alpha channel.</summary>
    internal TerminalColor SearchActiveMatchBackground { get; private set; }

    /// <summary>Selection background, with its alpha channel.</summary>
    internal TerminalColor SelectionBackgroundTransparent { get; private set; }

    /// <summary>Selection background blended with the background.</summary>
    internal TerminalColor SelectionBackgroundOpaque { get; private set; }

    /// <summary>Selection background of an unfocused terminal, with its alpha channel.</summary>
    internal TerminalColor SelectionInactiveBackgroundTransparent { get; private set; }

    /// <summary>Selection background of an unfocused terminal, blended with the background.</summary>
    internal TerminalColor SelectionInactiveBackgroundOpaque { get; private set; }

    /// <summary>The 256 ANSI colors.</summary>
    internal TerminalColor[] Ansi { get; private set; } = new TerminalColor[256];

    /// <summary>Cache of the foregrounds adjusted to the minimum contrast ratio; cleared when the theme changes.</summary>
    internal ColorContrastCache ContrastCache { get; } = new();

    /// <summary>The same for dim cells, which only need half the contrast.</summary>
    internal ColorContrastCache HalfContrastCache { get; } = new();

    /// <summary>Sets the theme; colors it does not define use the defaults. Drops the OSC overrides.</summary>
    /// <param name="theme">The theme, or <see langword="null"/> for the defaults.</param>
    internal void SetTheme(TerminalTheme? theme)
    {
        theme ??= new TerminalTheme();
        Foreground = ParseColor(theme.Foreground, DefaultForeground);
        Background = ParseColor(theme.Background, DefaultBackground);
        Cursor = ColorOps.Blend(Background, ParseColor(theme.Cursor, DefaultCursor));
        CursorAccent = ColorOps.Blend(Background, ParseColor(theme.CursorAccent, DefaultCursorAccent));
        SelectionBackgroundTransparent = ParseColor(theme.SelectionBackground, DefaultSelection);
        SelectionBackgroundOpaque = ColorOps.Blend(Background, SelectionBackgroundTransparent);
        SelectionInactiveBackgroundTransparent = ParseColor(theme.SelectionInactiveBackground, SelectionBackgroundTransparent);
        SelectionInactiveBackgroundOpaque = ColorOps.Blend(Background, SelectionInactiveBackgroundTransparent);
        SearchMatchBackground = ParseColor(theme.SearchMatchBackground, DefaultSearchMatch);
        SearchActiveMatchBackground = ParseColor(theme.SearchActiveMatchBackground, DefaultSearchActiveMatch);
        SelectionForeground = string.IsNullOrEmpty(theme.SelectionForeground) ? null : TryParseColor(theme.SelectionForeground);

        // If the selection color is opaque, blend it with the background with 0.3 opacity (xterm.js issue 2737).
        if (ColorOps.IsOpaque(SelectionBackgroundTransparent))
        {
            SelectionBackgroundTransparent = ColorOps.Opacity(SelectionBackgroundTransparent, 0.3);
        }

        if (ColorOps.IsOpaque(SelectionInactiveBackgroundTransparent))
        {
            SelectionInactiveBackgroundTransparent = ColorOps.Opacity(SelectionInactiveBackgroundTransparent, 0.3);
        }

        var ansi = DefaultAnsiColors.ToArray();
        string?[] named =
        [
            theme.Black, theme.Red, theme.Green, theme.Yellow, theme.Blue, theme.Magenta, theme.Cyan, theme.White,
            theme.BrightBlack, theme.BrightRed, theme.BrightGreen, theme.BrightYellow, theme.BrightBlue, theme.BrightMagenta,
            theme.BrightCyan, theme.BrightWhite,
        ];
        for (var i = 0; i < named.Length; i++)
        {
            ansi[i] = ParseColor(named[i], DefaultAnsiColors[i]);
        }

        if (theme.ExtendedAnsi is { } extended)
        {
            var count = Math.Min(ansi.Length - 16, extended.Count);
            for (var i = 0; i < count; i++)
            {
                ansi[i + 16] = ParseColor(extended[i], DefaultAnsiColors[i + 16]);
            }
        }

        Ansi = ansi;
        ContrastCache.Clear();
        HalfContrastCache.Clear();
        UpdateRestoreColors();
        Changed?.Invoke();
    }

    /// <summary>Restores a color to its theme value.</summary>
    /// <param name="slot">A palette index (0 to 255), a <see cref="SpecialColorIndex"/>, or <see langword="null"/> for the whole palette.</param>
    internal void RestoreColor(int? slot)
    {
        RestoreColorCore(slot);
        Changed?.Invoke();
    }

    /// <summary>Overrides a color.</summary>
    /// <param name="slot">A palette index (0 to 255) or a <see cref="SpecialColorIndex"/>.</param>
    /// <param name="color">The new color.</param>
    internal void SetColor(int slot, TerminalColor color)
    {
        switch (slot)
        {
            case SpecialColorIndex.Foreground:
                Foreground = color;
                break;
            case SpecialColorIndex.Background:
                Background = color;
                break;
            case SpecialColorIndex.Cursor:
                Cursor = color;
                break;
            default:
                Ansi[slot] = color;
                break;
        }

        Changed?.Invoke();
    }

    /// <summary>
    /// Applies the OSC color requests of the emulator (OSC 4, 10, 11, 12 and their 10x restore forms): set and restore
    /// change the palette, and each query produces the reply to send to the application.
    /// </summary>
    /// <param name="requests">The requests of one <c>OnColor</c> event.</param>
    /// <returns>The replies, in request order.</returns>
    internal IReadOnlyList<string> Apply(IReadOnlyList<ColorRequest> requests)
    {
        var replies = new List<string>();
        foreach (var request in requests)
        {
            switch (request)
            {
                case ColorReportRequest report:
                    var rgb = ColorOps.ToColorRgb(GetColor(report.Index));
                    replies.Add($"\u001b]{Identifier(report.Index)};{XParseColor.ToRgbString(rgb)}\u001b\\");
                    break;
                case ColorSetRequest set:
                    SetColor(set.Index, Channels.ToColor(set.Color.R, set.Color.G, set.Color.B));
                    break;
                case ColorRestoreRequest restore:
                    RestoreColor(restore.Index);
                    break;
            }
        }

        return replies;
    }

    private static string Identifier(int index) => index switch
    {
        SpecialColorIndex.Foreground => "10",
        SpecialColorIndex.Background => "11",
        SpecialColorIndex.Cursor => "12",
        _ => "4;" + index.ToString(System.Globalization.CultureInfo.InvariantCulture),
    };

    private static TerminalColor ParseColor(string? css, TerminalColor fallback) => TryParseColor(css) ?? fallback;

    private static TerminalColor? TryParseColor(string? css)
    {
        if (css is not null)
        {
            try
            {
                return Css.ToColor(css);
            }
            catch (ArgumentException)
            {
                // Invalid CSS: the caller uses its fallback.
            }
        }

        return null;
    }

    private static TerminalColor[] CreateDefaultAnsiColors()
    {
        string[] named =
        [
            "#2e3436", "#cc0000", "#4e9a06", "#c4a000", "#3465a4", "#75507b", "#06989a", "#d3d7cf", // dark
            "#555753", "#ef2929", "#8ae234", "#fce94f", "#729fcf", "#ad7fa8", "#34e2e2", "#eeeeec", // bright
        ];
        var colors = new List<TerminalColor>(256);
        colors.AddRange(named.Select(Css.ToColor));

        // Colors 16-231: the 6x6x6 cube.
        int[] v = [0x00, 0x5f, 0x87, 0xaf, 0xd7, 0xff];
        for (var i = 0; i < 216; i++)
        {
            colors.Add(Channels.ToColor(v[(i / 36) % 6], v[(i / 6) % 6], v[i % 6]));
        }

        // Colors 232-255: grays.
        for (var i = 0; i < 24; i++)
        {
            var c = 8 + (i * 10);
            colors.Add(Channels.ToColor(c, c, c));
        }

        return [.. colors];
    }

    private TerminalColor GetColor(int index) => index switch
    {
        SpecialColorIndex.Foreground => Foreground,
        SpecialColorIndex.Background => Background,
        SpecialColorIndex.Cursor => Cursor,
        _ => Ansi[index],
    };

    private void RestoreColorCore(int? slot)
    {
        // An unset slot restores all ansi colors.
        if (slot is not { } value)
        {
            Array.Copy(_restoreAnsi, Ansi, _restoreAnsi.Length);
            return;
        }

        switch (value)
        {
            case SpecialColorIndex.Foreground:
                Foreground = _restoreForeground;
                break;
            case SpecialColorIndex.Background:
                Background = _restoreBackground;
                break;
            case SpecialColorIndex.Cursor:
                Cursor = _restoreCursor;
                break;
            default:
                Ansi[value] = _restoreAnsi[value];
                break;
        }
    }

    private void UpdateRestoreColors()
    {
        _restoreForeground = Foreground;
        _restoreBackground = Background;
        _restoreCursor = Cursor;
        _restoreAnsi = (TerminalColor[])Ansi.Clone();
    }
}
