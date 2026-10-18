// Ported from xtermjs/xterm.js: src/common/data/Charsets.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Core.Data;

/// <summary>
/// The character sets supported by the terminal. These enable several languages to be represented within the terminal
/// with only 8-bit encoding. See ISO 2022 for a discussion on character sets. Only VT100 character sets are supported.
/// </summary>
/// <remarks>Threading: immutable tables; safe to share between threads.</remarks>
internal static class Charsets
{
    private static readonly Dictionary<char, string> DecSpecialGraphics = new()
    {
        ['`'] = "\u25c6",
        ['a'] = "\u2592",
        ['b'] = "\u2409",
        ['c'] = "\u240c",
        ['d'] = "\u240d",
        ['e'] = "\u240a",
        ['f'] = "\u00b0",
        ['g'] = "\u00b1",
        ['h'] = "\u2424",
        ['i'] = "\u240b",
        ['j'] = "\u2518",
        ['k'] = "\u2510",
        ['l'] = "\u250c",
        ['m'] = "\u2514",
        ['n'] = "\u253c",
        ['o'] = "\u23ba",
        ['p'] = "\u23bb",
        ['q'] = "\u2500",
        ['r'] = "\u23bc",
        ['s'] = "\u23bd",
        ['t'] = "\u251c",
        ['u'] = "\u2524",
        ['v'] = "\u2534",
        ['w'] = "\u252c",
        ['x'] = "\u2502",
        ['y'] = "\u2264",
        ['z'] = "\u2265",
        ['{'] = "\u03c0",
        ['|'] = "\u2260",
        ['}'] = "\u00a3",
        ['~'] = "\u00b7",
    };

    private static readonly Dictionary<char, string> British = new()
    {
        ['#'] = "£",
    };

    private static readonly Dictionary<char, string> Dutch = new()
    {
        ['#'] = "£",
        ['@'] = "¾",
        ['['] = "ij",
        ['\\'] = "½",
        [']'] = "|",
        ['{'] = "¨",
        ['|'] = "f",
        ['}'] = "¼",
        ['~'] = "´",
    };

    private static readonly Dictionary<char, string> Finnish = new()
    {
        ['['] = "Ä",
        ['\\'] = "Ö",
        [']'] = "Å",
        ['^'] = "Ü",
        ['`'] = "é",
        ['{'] = "ä",
        ['|'] = "ö",
        ['}'] = "å",
        ['~'] = "ü",
    };

    private static readonly Dictionary<char, string> French = new()
    {
        ['#'] = "£",
        ['@'] = "à",
        ['['] = "°",
        ['\\'] = "ç",
        [']'] = "§",
        ['{'] = "é",
        ['|'] = "ù",
        ['}'] = "è",
        ['~'] = "¨",
    };

    private static readonly Dictionary<char, string> FrenchCanadian = new()
    {
        ['@'] = "à",
        ['['] = "â",
        ['\\'] = "ç",
        [']'] = "ê",
        ['^'] = "î",
        ['`'] = "ô",
        ['{'] = "é",
        ['|'] = "ù",
        ['}'] = "è",
        ['~'] = "û",
    };

    private static readonly Dictionary<char, string> German = new()
    {
        ['@'] = "§",
        ['['] = "Ä",
        ['\\'] = "Ö",
        [']'] = "Ü",
        ['{'] = "ä",
        ['|'] = "ö",
        ['}'] = "ü",
        ['~'] = "ß",
    };

    private static readonly Dictionary<char, string> Italian = new()
    {
        ['#'] = "£",
        ['@'] = "§",
        ['['] = "°",
        ['\\'] = "ç",
        [']'] = "é",
        ['`'] = "ù",
        ['{'] = "à",
        ['|'] = "ò",
        ['}'] = "è",
        ['~'] = "ì",
    };

    private static readonly Dictionary<char, string> NorwegianDanish = new()
    {
        ['@'] = "Ä",
        ['['] = "Æ",
        ['\\'] = "Ø",
        [']'] = "Å",
        ['^'] = "Ü",
        ['`'] = "ä",
        ['{'] = "æ",
        ['|'] = "ø",
        ['}'] = "å",
        ['~'] = "ü",
    };

    private static readonly Dictionary<char, string> Spanish = new()
    {
        ['#'] = "£",
        ['@'] = "§",
        ['['] = "¡",
        ['\\'] = "Ñ",
        [']'] = "¿",
        ['{'] = "°",
        ['|'] = "ñ",
        ['}'] = "ç",
    };

    private static readonly Dictionary<char, string> Swedish = new()
    {
        ['@'] = "É",
        ['['] = "Ä",
        ['\\'] = "Ö",
        [']'] = "Å",
        ['^'] = "Ü",
        ['`'] = "é",
        ['{'] = "ä",
        ['|'] = "ö",
        ['}'] = "å",
        ['~'] = "ü",
    };

    private static readonly Dictionary<char, string> Swiss = new()
    {
        ['#'] = "ù",
        ['@'] = "à",
        ['['] = "é",
        ['\\'] = "ç",
        [']'] = "ê",
        ['^'] = "î",
        ['_'] = "è",
        ['`'] = "ô",
        ['{'] = "ä",
        ['|'] = "ö",
        ['}'] = "ü",
        ['~'] = "û",
    };

    /// <summary>The character sets by designator character; <c>null</c> is US ASCII, which has no table.</summary>
    public static IReadOnlyDictionary<char, IReadOnlyDictionary<char, string>?> All { get; } =
        new Dictionary<char, IReadOnlyDictionary<char, string>?>
        {
            ['0'] = DecSpecialGraphics,
            ['4'] = Dutch,
            ['5'] = Finnish,
            ['6'] = NorwegianDanish,
            ['7'] = Swedish,
            ['='] = Swiss,
            ['A'] = British,
            ['B'] = null,
            ['C'] = Finnish,
            ['E'] = NorwegianDanish,
            ['H'] = Swedish,
            ['K'] = German,
            ['Q'] = FrenchCanadian,
            ['R'] = French,
            ['Y'] = Italian,
            ['Z'] = Spanish,
        };

    /// <summary>The default character set, US (no table).</summary>
    public static IReadOnlyDictionary<char, string>? DefaultCharset => null;
}
