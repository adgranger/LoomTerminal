// Ported from xtermjs/xterm.js: src/common/services/OptionsService.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using System.Globalization;
using LoomTerminal.Core.Common;

namespace LoomTerminal.Core.Services;

/// <summary>
/// Holds the terminal options as an immutable <see cref="TerminalOptions"/>. Changes go through
/// <see cref="SetOptions"/>, which validates every property and then raises one event per changed property.
/// </summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal sealed class OptionsService : Disposable, IOptionsService
{
    private readonly Emitter<TerminalOption> _onOptionChange = new();

    /// <summary>
    /// Creates the service. An invalid property of <paramref name="options"/> falls back to its default value, like
    /// xterm.js does for invalid constructor options.
    /// </summary>
    public OptionsService(TerminalOptions? options = null)
    {
        Register(_onOptionChange);
        RawOptions = Sanitize(options ?? TerminalOptions.Default, useDefaultOnError: true);
    }

    /// <inheritdoc />
    public TerminalOptions RawOptions { get; private set; }

    /// <inheritdoc />
    public TerminalOptions Options => RawOptions;

    /// <inheritdoc />
    public IEvent<TerminalOption> OnOptionChange => _onOptionChange.Event;

    /// <summary>The properties that differ between <paramref name="a"/> and <paramref name="b"/>, in declaration order.</summary>
    public static IEnumerable<TerminalOption> Diff(TerminalOptions a, TerminalOptions b)
    {
        if (a.Cols != b.Cols)
        {
            yield return TerminalOption.Cols;
        }

        if (a.Rows != b.Rows)
        {
            yield return TerminalOption.Rows;
        }

        if (a.CursorBlink != b.CursorBlink)
        {
            yield return TerminalOption.CursorBlink;
        }

        if (a.CursorStyle != b.CursorStyle)
        {
            yield return TerminalOption.CursorStyle;
        }

        if (a.CursorInactiveStyle != b.CursorInactiveStyle)
        {
            yield return TerminalOption.CursorInactiveStyle;
        }

        if (a.CursorWidth != b.CursorWidth)
        {
            yield return TerminalOption.CursorWidth;
        }

        if (a.DrawBoldTextInBrightColors != b.DrawBoldTextInBrightColors)
        {
            yield return TerminalOption.DrawBoldTextInBrightColors;
        }

        if (a.FastScrollModifier != b.FastScrollModifier)
        {
            yield return TerminalOption.FastScrollModifier;
        }

        if (a.FastScrollSensitivity != b.FastScrollSensitivity)
        {
            yield return TerminalOption.FastScrollSensitivity;
        }

        if (a.IgnoreBracketedPasteMode != b.IgnoreBracketedPasteMode)
        {
            yield return TerminalOption.IgnoreBracketedPasteMode;
        }

        if (a.LogLevel != b.LogLevel)
        {
            yield return TerminalOption.LogLevel;
        }

        if (a.MacOptionIsMeta != b.MacOptionIsMeta)
        {
            yield return TerminalOption.MacOptionIsMeta;
        }

        if (a.MinimumContrastRatio != b.MinimumContrastRatio)
        {
            yield return TerminalOption.MinimumContrastRatio;
        }

        if (a.DisableStdin != b.DisableStdin)
        {
            yield return TerminalOption.DisableStdin;
        }

        if (a.RightClickSelectsWord != b.RightClickSelectsWord)
        {
            yield return TerminalOption.RightClickSelectsWord;
        }

        if (a.ScreenReaderMode != b.ScreenReaderMode)
        {
            yield return TerminalOption.ScreenReaderMode;
        }

        if (a.Scrollback != b.Scrollback)
        {
            yield return TerminalOption.Scrollback;
        }

        if (a.ScrollOnEraseInDisplay != b.ScrollOnEraseInDisplay)
        {
            yield return TerminalOption.ScrollOnEraseInDisplay;
        }

        if (a.ScrollOnUserInput != b.ScrollOnUserInput)
        {
            yield return TerminalOption.ScrollOnUserInput;
        }

        if (a.ScrollSensitivity != b.ScrollSensitivity)
        {
            yield return TerminalOption.ScrollSensitivity;
        }

        if (a.TabStopWidth != b.TabStopWidth)
        {
            yield return TerminalOption.TabStopWidth;
        }

        if (a.ReflowCursorLine != b.ReflowCursorLine)
        {
            yield return TerminalOption.ReflowCursorLine;
        }

        if (a.WindowOptions != b.WindowOptions)
        {
            yield return TerminalOption.WindowOptions;
        }

        if (a.WindowsMode != b.WindowsMode)
        {
            yield return TerminalOption.WindowsMode;
        }

        if (a.WindowsPty != b.WindowsPty)
        {
            yield return TerminalOption.WindowsPty;
        }

        if (a.WordSeparator != b.WordSeparator)
        {
            yield return TerminalOption.WordSeparator;
        }

        if (a.AltClickMovesCursor != b.AltClickMovesCursor)
        {
            yield return TerminalOption.AltClickMovesCursor;
        }

        if (a.ConvertEol != b.ConvertEol)
        {
            yield return TerminalOption.ConvertEol;
        }

        if (a.TermName != b.TermName)
        {
            yield return TerminalOption.TermName;
        }
    }

    /// <inheritdoc />
    public void SetOptions(TerminalOptions value)
    {
        TerminalOptions validated = Sanitize(value, useDefaultOnError: false);
        TerminalOptions old = RawOptions;
        RawOptions = validated;

        // Don't fire an option change event if they didn't change
        foreach (TerminalOption key in Diff(old, validated))
        {
            _onOptionChange.Fire(key);
        }
    }

    /// <inheritdoc />
    public IDisposable OnSpecificOptionChange(TerminalOption key, Action listener) =>
        _onOptionChange.Event.Subscribe(eventKey =>
        {
            if (eventKey == key)
            {
                listener();
            }
        });

    /// <inheritdoc />
    public IDisposable OnMultipleOptionChange(IReadOnlyList<TerminalOption> keys, Action listener) =>
        _onOptionChange.Event.Subscribe(eventKey =>
        {
            for (int i = 0; i < keys.Count; i++)
            {
                if (keys[i] == eventKey)
                {
                    listener();
                    return;
                }
            }
        });

    // Port of _sanitizeAndValidateOption for the Core subset. With useDefaultOnError, a property that fails
    // validation takes its default value (constructor behavior), otherwise the exception is thrown.
    private static TerminalOptions Sanitize(TerminalOptions value, bool useDefaultOnError)
    {
        TerminalOptions defaults = TerminalOptions.Default;
        return value with
        {
            CursorStyle = Run(() => SanitizeCursorStyle(value.CursorStyle), defaults.CursorStyle, useDefaultOnError),
            WordSeparator = string.IsNullOrEmpty(value.WordSeparator) ? defaults.WordSeparator : value.WordSeparator,
            CursorWidth = Run(
                () => AtLeastOne("cursorWidth", Math.Floor(value.CursorWidth)),
                defaults.CursorWidth,
                useDefaultOnError),
            TabStopWidth = Run(
                () => (int)AtLeastOne("tabStopWidth", value.TabStopWidth),
                defaults.TabStopWidth,
                useDefaultOnError),
            MinimumContrastRatio = Math.Max(1, Math.Min(21, Math.Floor((value.MinimumContrastRatio * 10) + 0.5) / 10)),
            Scrollback = Run(() => SanitizeScrollback(value.Scrollback), defaults.Scrollback, useDefaultOnError),
            FastScrollSensitivity = Run(
                () => Positive("fastScrollSensitivity", value.FastScrollSensitivity),
                defaults.FastScrollSensitivity,
                useDefaultOnError),
            ScrollSensitivity = Run(
                () => Positive("scrollSensitivity", value.ScrollSensitivity),
                defaults.ScrollSensitivity,
                useDefaultOnError),
            WindowsPty = value.WindowsPty ?? new WindowsPtyOptions(),
            WindowOptions = value.WindowOptions ?? new WindowOptions(),
        };
    }

    private static T Run<T>(Func<T> sanitize, T fallback, bool useDefaultOnError)
    {
        if (!useDefaultOnError)
        {
            return sanitize();
        }

        try
        {
            return sanitize();
        }
        catch (ArgumentException)
        {
            return fallback;
        }
    }

    private static CursorStyle SanitizeCursorStyle(CursorStyle value) =>
        value is CursorStyle.Block or CursorStyle.Underline or CursorStyle.Bar
            ? value
            : throw new ArgumentException($"\"{(int)value}\" is not a valid value for cursorStyle");

    private static double AtLeastOne(string key, double value) =>
        value < 1
            ? throw new ArgumentException($"{key} cannot be less than 1, value: {Format(value)}")
            : value;

    private static double Positive(string key, double value) =>
        value <= 0
            ? throw new ArgumentException($"{key} cannot be less than or equal to 0, value: {Format(value)}")
            : value;

    private static long SanitizeScrollback(long value)
    {
        value = Math.Min(value, 4294967295);
        return value < 0
            ? throw new ArgumentException($"scrollback cannot be less than 0, value: {Format(value)}")
            : value;
    }

    private static string Format(double value) => value.ToString(CultureInfo.InvariantCulture);
}
