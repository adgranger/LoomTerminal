using System.Globalization;

namespace LoomTerminal.Gallery.Diagnostics;

/// <summary>Statistics of the intervals between rendered frames.</summary>
/// <param name="Count">Number of intervals.</param>
/// <param name="AverageFps">Average frames per second.</param>
/// <param name="Average">Average interval, in milliseconds.</param>
/// <param name="P95">95th percentile interval, in milliseconds.</param>
/// <param name="Max">Longest interval, in milliseconds.</param>
/// <remarks>Threading: immutable; pure computation.</remarks>
// Inspired by LoomEditor: samples/LoomEditor.Gallery/FrameStatistics.cs
internal readonly record struct FrameStatistics(int Count, double AverageFps, double Average, double P95, double Max)
{
    /// <summary>Computes the statistics of <paramref name="intervalsMilliseconds"/>.</summary>
    public static FrameStatistics Compute(IReadOnlyList<double> intervalsMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(intervalsMilliseconds);

        if (intervalsMilliseconds.Count == 0)
        {
            return default;
        }

        double[] sorted = [.. intervalsMilliseconds];
        Array.Sort(sorted);
        double total = sorted.Sum();
        return new FrameStatistics(
            sorted.Length,
            total > 0 ? sorted.Length * 1000.0 / total : 0,
            total / sorted.Length,
            sorted[Math.Max(0, (int)Math.Ceiling(0.95 * sorted.Length) - 1)], // nearest rank
            sorted[^1]);
    }

    /// <summary>Formats the statistics for the status line.</summary>
    public string Format() => string.Create(
        CultureInfo.InvariantCulture,
        $"{Count} frames · {AverageFps:0.0} fps · avg {Average:0.0} ms · p95 {P95:0.0} ms · max {Max:0.0} ms");
}
