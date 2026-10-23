using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using LoomTerminal.Presentation.Session;
using LoomTerminal.Presentation.Theming;
using SkiaSharp;

namespace LoomTerminal.Avalonia.Controls;

/// <summary>
/// The terminal control for Avalonia. Placeholder until P6-02: it only proves the Skia draw path.
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
public sealed class TerminalView : Control
{
    /// <summary>Identifies <see cref="TerminalTheme"/>.</summary>
    public static readonly StyledProperty<TerminalTheme> TerminalThemeProperty =
        AvaloniaProperty.Register<TerminalView, TerminalTheme>(nameof(TerminalTheme), new TerminalTheme());

    /// <summary>Identifies <see cref="LineHeight"/>.</summary>
    public static readonly StyledProperty<double> LineHeightProperty =
        AvaloniaProperty.Register<TerminalView, double>(nameof(LineHeight), 1.15);

    /// <summary>Identifies <see cref="CursorStyle"/>.</summary>
    public static readonly StyledProperty<TerminalCursorStyle> CursorStyleProperty =
        AvaloniaProperty.Register<TerminalView, TerminalCursorStyle>(nameof(CursorStyle), TerminalCursorStyle.Block);

    /// <summary>Identifies <see cref="CursorBlink"/>.</summary>
    public static readonly StyledProperty<bool> CursorBlinkProperty =
        AvaloniaProperty.Register<TerminalView, bool>(nameof(CursorBlink));

    static TerminalView()
    {
        AffectsRender<TerminalView>(TerminalThemeProperty, LineHeightProperty, CursorStyleProperty, CursorBlinkProperty);
    }

    /// <summary>The colors of the terminal.</summary>
    public TerminalTheme TerminalTheme
    {
        get => GetValue(TerminalThemeProperty);
        set => SetValue(TerminalThemeProperty, value);
    }

    /// <summary>Line height as a multiple of the font size (default 1.15).</summary>
    public double LineHeight
    {
        get => GetValue(LineHeightProperty);
        set => SetValue(LineHeightProperty, value);
    }

    /// <summary>The shape of the cursor.</summary>
    public TerminalCursorStyle CursorStyle
    {
        get => GetValue(CursorStyleProperty);
        set => SetValue(CursorStyleProperty, value);
    }

    /// <summary>Whether the cursor blinks.</summary>
    public bool CursorBlink
    {
        get => GetValue(CursorBlinkProperty);
        set => SetValue(CursorBlinkProperty, value);
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context) =>
        context.Custom(new PlaceholderDrawOperation(new Rect(Bounds.Size)));

    private sealed class PlaceholderDrawOperation(Rect bounds) : ICustomDrawOperation
    {
        public Rect Bounds => bounds;

        public bool HitTest(Point p) => bounds.Contains(p);

        public bool Equals(ICustomDrawOperation? other) => false;

        public void Render(ImmediateDrawingContext context)
        {
            if (context.TryGetFeature<ISkiaSharpApiLeaseFeature>() is not { } feature)
            {
                return;
            }

            using ISkiaSharpApiLease lease = feature.Lease();
            SKCanvas canvas = lease.SkCanvas;
            canvas.Clear(new SKColor(0x1e, 0x1e, 0x1e));
            using var font = new SKFont(SKTypeface.FromFamilyName("Consolas"), 16);
            using var paint = new SKPaint { Color = new SKColor(0xcc, 0xcc, 0xcc), IsAntialias = true };
            canvas.DrawText("LoomTerminal.Avalonia", 8, 24, font, paint);
        }

        public void Dispose()
        {
        }
    }
}
