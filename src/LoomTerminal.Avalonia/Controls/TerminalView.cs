using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using LoomTerminal.Avalonia.Rendering;
using LoomTerminal.Presentation.Rendering;
using LoomTerminal.Presentation.Session;
using LoomTerminal.Presentation.Theming;

namespace LoomTerminal.Avalonia.Controls;

/// <summary>The terminal control for Avalonia, drawn with Skia.</summary>
/// <remarks>
/// <para>
/// In progress (phase 6): the grid is drawn and sized to the control; input and the scrollbar come with the next
/// cards, see docs/ROADMAP.md.
/// </para>
/// <para>Threading: like every Avalonia control, use it on the UI thread only.</para>
/// </remarks>
public sealed partial class TerminalView : TemplatedControl
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

    private readonly SkiaTextBlobCache _blobs = new(256);
    private readonly SkiaTextMeasurer _measurer = new();
    private TerminalFont? _font;
    private string? _family;
    private RenderFrame? _lastFrame;

    static TerminalView()
    {
        FontFamilyProperty.OverrideDefaultValue<TerminalView>(new FontFamily("Cascadia Mono, Consolas, Menlo, DejaVu Sans Mono"));
        FontSizeProperty.OverrideDefaultValue<TerminalView>(14.0);
        FocusableProperty.OverrideDefaultValue<TerminalView>(true);
        TemplateProperty.OverrideDefaultValue<TerminalView>(DefaultTemplate());
    }

    /// <summary>Creates the control.</summary>
    public TerminalView()
    {
        InitializeFocus();
        InitializeKeyboard();
        Cursor = new Cursor(StandardCursorType.Arrow);
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

    private TerminalFont CurrentFont() =>
        _font ??= new(FontFamily.FamilyNames.ToArray(), FontSize, LineHeight, Weight: (int)FontWeight);

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == FontFamilyProperty || change.Property == FontSizeProperty
            || change.Property == LineHeightProperty || change.Property == FontWeightProperty)
        {
            _font = null;
            _family = null;
            _blobs.Clear();
            RequestFrame();
        }
        else if (change.Property == PaddingProperty || change.Property == BoundsProperty)
        {
            RequestFrame();
        }
        else if (change.Property == TerminalThemeProperty)
        {
            // The blob cache does not key on colors: just redraw.
            _session?.SetTheme(TerminalTheme);
            RequestFrame();
        }
        else if (change.Property == CursorStyleProperty)
        {
            _session?.SetCursorStyle(CursorStyle);
        }
        else if (change.Property == CursorBlinkProperty)
        {
            _session?.SetCursorBlink(CursorBlink);
            RequestFrame();
        }
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        TerminalFont font = CurrentFont();
        CellMetrics metrics = _measurer.Measure(font, _scale);
        RenderFrame? frame = _session?.TryTakeFrame() ?? _lastFrame;
        if (frame is null)
        {
            return;
        }

        _lastFrame = frame;
        // Changing the scrollbar invalidates visuals, which is forbidden while rendering: defer it.
        Dispatcher.UIThread.Post(() => UpdateScrollBar(frame));
        // 4 x rows x an average of 4 runs per row.
        _blobs.Capacity = 16 * Math.Max(1, frame.Lines.Count);
        var bounds = new Rect(Bounds.Size);
        using (context.PushTransform(Matrix.CreateTranslation(Padding.Left, Padding.Top)))
        {
            context.Custom(new TerminalDrawOperation(
                bounds, frame, metrics, _blobs, font, _family ??= SkiaTextMeasurer.ResolveFamily(font),
                _scale, _session?.CustomGlyphs ?? true, _composition));
        }
    }
}
