using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using LoomTerminal.Presentation.Rendering;

namespace LoomTerminal.Avalonia.Rendering;

/// <summary>Draws one terminal frame on the Skia canvas of Avalonia.</summary>
/// <remarks>Threading: created on the UI thread, <see cref="Render"/> runs on the render thread; it only reads the immutable frame.</remarks>
internal sealed class TerminalDrawOperation(
    Rect bounds,
    RenderFrame frame,
    CellMetrics metrics,
    SkiaTextBlobCache cache,
    TerminalFont font,
    string family,
    double scale,
    bool customGlyphs,
    string? compositionText) : ICustomDrawOperation
{
    /// <inheritdoc />
    public Rect Bounds => bounds;

    /// <inheritdoc />
    public bool HitTest(Point p) => bounds.Contains(p);

    /// <inheritdoc />
    public bool Equals(ICustomDrawOperation? other) => false;

    /// <inheritdoc />
    public void Render(ImmediateDrawingContext context)
    {
        if (context.TryGetFeature<ISkiaSharpApiLeaseFeature>() is not { } feature)
        {
            return;
        }

        using ISkiaSharpApiLease lease = feature.Lease();
        using var surface = new SkiaRenderSurface(lease.SkCanvas, cache, font, family, scale);
        FrameRenderer.Render(frame, metrics, surface, customGlyphs, compositionText);
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
