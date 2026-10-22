// Ported from xtermjs/xterm.js: addons/addon-webgl/src/CustomGlyphs.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.
using System.Collections.Concurrent;
using System.Globalization;

namespace LoomTerminal.Presentation.Rendering;

/// <summary>
/// Box drawing, block elements, shades and powerline symbols as vectors filling the cell exactly. Upstream draws
/// them on a canvas 2D context (<c>tryDrawCustomChar</c>); here each canvas call becomes one
/// <see cref="CustomGlyphCommand"/> and the backend replays them.
/// </summary>
/// <remarks>Threading: stateless apart from a thread-safe cache; any thread may call it.</remarks>
internal static partial class CustomGlyphs
{
    private const double NormalWeight = 1;
    private const double BoldWeight = 3;
    private const int MaxCachedPaths = 2048;

    private static readonly ConcurrentDictionary<(int CodePoint, double Width, double Height), CustomGlyphPath?> Cache = new();

    private sealed record BoxShape(Func<double, double, string>? Normal, Func<double, double, string>? Bold);

    private readonly record struct PowerlineShape(string D, bool IsStroke, int LeftPadding, int RightPadding);

    /// <summary>Whether <paramref name="codePoint"/> has a vector definition.</summary>
    /// <param name="codePoint">The Unicode scalar value.</param>
    /// <returns><see langword="true"/> when <see cref="TryGetPath"/> returns a path.</returns>
    public static bool IsCustomGlyph(int codePoint) =>
        BlockElementDefinitions.ContainsKey(codePoint) || PatternAlphas.ContainsKey(codePoint)
        || BoxDrawingDefinitions.ContainsKey(codePoint) || PowerlineDefinitions.ContainsKey(codePoint);

    /// <summary>Builds the drawing of a character for a cell size.</summary>
    /// <param name="codePoint">The Unicode scalar value.</param>
    /// <param name="metrics">The cell metrics; only the size is used.</param>
    /// <returns>The path, or <see langword="null"/> when the character has no definition.</returns>
    public static CustomGlyphPath? TryGetPath(int codePoint, in CellMetrics metrics)
    {
        if (!IsCustomGlyph(codePoint))
        {
            return null;
        }

        if (Cache.Count > MaxCachedPaths)
        {
            Cache.Clear();
        }

        return Cache.GetOrAdd((codePoint, metrics.Width, metrics.Height), static key => Build(key.CodePoint, key.Width, key.Height));
    }

    private static CustomGlyphPath? Build(int codePoint, double width, double height)
    {
        var commands = new List<CustomGlyphCommand>();
        if (BlockElementDefinitions.TryGetValue(codePoint, out var blocks))
        {
            // drawBlockElementChar
            var xEighth = width / 8;
            var yEighth = height / 8;
            for (var i = 0; i < blocks.Length; i += 4)
            {
                commands.Add(new(CustomGlyphCommandKind.Rect, blocks[i] * xEighth, blocks[i + 1] * yEighth, blocks[i + 2] * xEighth, blocks[i + 3] * yEighth));
            }

            commands.Add(new(CustomGlyphCommandKind.Fill, 1));
            return new CustomGlyphPath(commands, false);
        }

        if (PatternAlphas.TryGetValue(codePoint, out var alpha))
        {
            commands.Add(new(CustomGlyphCommandKind.Rect, 0, 0, width, height));
            commands.Add(new(CustomGlyphCommandKind.Fill, alpha));
            return new CustomGlyphPath(commands, false);
        }

        if (BoxDrawingDefinitions.TryGetValue(codePoint, out var box))
        {
            // drawBoxDrawingChar: one stroke per weight, the normal one first.
            AddBoxStroke(commands, box.Normal, NormalWeight, width, height);
            AddBoxStroke(commands, box.Bold, BoldWeight, width, height);
            return new CustomGlyphPath(commands, false);
        }

        if (PowerlineDefinitions.TryGetValue(codePoint, out var powerline))
        {
            // drawPowerlineChar.
            // shortcut: the stroke width comes from the cell height (font size / 12 with a 1.2 line height) because the frame carries no font size; pass it down if a custom line height looks wrong.
            var cssLineWidth = height / 14.4;
            AddPath(commands, powerline.D, width, height, false, powerline.LeftPadding * (cssLineWidth / 2), powerline.RightPadding * (cssLineWidth / 2));
            commands.Add(powerline.IsStroke
                ? new(CustomGlyphCommandKind.Stroke, cssLineWidth)
                : new(CustomGlyphCommandKind.Fill, 1));
            return new CustomGlyphPath(commands, true);
        }

        return null;
    }

    private static void AddBoxStroke(List<CustomGlyphCommand> commands, Func<double, double, string>? instructions, double weight, double width, double height)
    {
        if (instructions is null)
        {
            return;
        }

        const double xp = .15;
        var yp = .15 / height * width;
        AddPath(commands, instructions(xp, yp), width, height, true, 0, 0);
        commands.Add(new(CustomGlyphCommandKind.Stroke, weight));
    }

    // The loop of drawBoxDrawingChar / drawPowerlineChar: SVG-like instructions (M, L, C) to commands.
    private static void AddPath(List<CustomGlyphCommand> commands, string instructions, double width, double height, bool clamp, double leftPadding, double rightPadding)
    {
        foreach (var instruction in instructions.Split(' '))
        {
            var kind = instruction[0] switch
            {
                'M' => CustomGlyphCommandKind.MoveTo,
                'L' => CustomGlyphCommandKind.LineTo,
                'C' => CustomGlyphCommandKind.CurveTo,
                _ => throw new InvalidOperationException($"Could not find drawing instructions for \"{instruction[0]}\""),
            };
            var args = instruction[1..].Split(',');
            if (args[0].Length == 0 || args[1].Length == 0)
            {
                continue;
            }

            var v = TranslateArgs(args, width, height, clamp, leftPadding, rightPadding);
            commands.Add(new(kind, v[0], v[1], v.Length > 2 ? v[2] : 0, v.Length > 3 ? v[3] : 0, v.Length > 4 ? v[4] : 0, v.Length > 5 ? v[5] : 0));
        }
    }

    private static double[] TranslateArgs(string[] args, double cellWidth, double cellHeight, bool doClamp, double leftPadding, double rightPadding)
    {
        var result = new double[args.Length];
        for (var i = 0; i < args.Length; i++)
        {
            result[i] = double.Parse(args[i], NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        if (result.Length < 2)
        {
            throw new InvalidOperationException("Too few arguments for instruction");
        }

        for (var x = 0; x < result.Length; x += 2)
        {
            // Translate from 0-1 to 0-cellWidth.
            result[x] *= cellWidth - leftPadding - rightPadding;
            // Keep the coordinate in the cell and snap to the nearest 0.5 for a crisp line at 100 % scale.
            // JS Math.round(v + 0.5) is Math.Floor(v + 1).
            if (doClamp && result[x] != 0)
            {
                result[x] = Clamp(Math.Floor(result[x] + 1) - 0.5, cellWidth);
            }

            result[x] += leftPadding;
        }

        for (var y = 1; y < result.Length; y += 2)
        {
            result[y] *= cellHeight;
            if (doClamp && result[y] != 0)
            {
                result[y] = Clamp(Math.Floor(result[y] + 1) - 0.5, cellHeight);
            }
        }

        return result;
    }

    // JS clamp(value, max) with min = 0.
    private static double Clamp(double value, double max) => Math.Max(Math.Min(value, max), 0);
}
