using System.Text;
using LoomTerminal.Core.Buffer;

namespace LoomTerminal.Core.Tests.Conformance;

/// <summary>The cursor of a <see cref="ScreenSnapshot"/>.</summary>
internal sealed record CursorSnapshot(int X, int Y, bool Visible);

/// <summary>The terminal modes of a <see cref="ScreenSnapshot"/>.</summary>
internal sealed record ModesSnapshot(
    bool ApplicationCursorKeys,
    bool BracketedPaste,
    bool Insert,
    bool Origin,
    bool Wraparound,
    string MouseTracking);

/// <summary>A group of consecutive cells of a line that share the same attributes.</summary>
internal sealed record RunSnapshot(int Start, int Length, string Fg, string Bg, IReadOnlyList<string> Flags);

/// <summary>A row of the viewport.</summary>
internal sealed record LineSnapshot(string Text, bool Wrapped, IReadOnlyList<RunSnapshot> Runs);

/// <summary>
/// The state of a terminal screen in the JSON format shared with the xterm.js oracle (see
/// <c>docs/tasks/phase-2/README.md</c>).
/// </summary>
/// <remarks>Threading: immutable; <see cref="Capture"/> must not run while another thread writes to the terminal.</remarks>
internal sealed record ScreenSnapshot(
    int Cols,
    int Rows,
    CursorSnapshot Cursor,
    string ActiveBuffer,
    ModesSnapshot Modes,
    string Title,
    IReadOnlyList<LineSnapshot> Lines)
{
    /// <summary>Captures the viewport of <paramref name="core"/>; <paramref name="title"/> is the last OSC title it announced.</summary>
    internal static ScreenSnapshot Capture(TerminalCore core, string title = "")
    {
        Core.Buffer.Buffer buffer = core.Buffers.Active;
        var lines = new List<LineSnapshot>(core.Rows);
        for (int y = 0; y < core.Rows; y++)
        {
            lines.Add(CaptureLine((BufferLine)buffer.Lines.Get(buffer.YBase + y)!));
        }

        var modes = new ModesSnapshot(
            core.CoreService.DecPrivateModes.ApplicationCursorKeys,
            core.CoreService.DecPrivateModes.BracketedPasteMode,
            core.CoreService.Modes.InsertMode,
            core.CoreService.DecPrivateModes.Origin,
            core.CoreService.DecPrivateModes.Wraparound,
            core.CoreMouseService.ActiveProtocol.ToLowerInvariant());

        return new ScreenSnapshot(
            core.Cols,
            core.Rows,
            new CursorSnapshot(buffer.X, buffer.Y, !core.CoreService.IsCursorHidden),
            ReferenceEquals(buffer, core.Buffers.Alt) ? "alternate" : "normal",
            modes,
            title,
            lines);
    }

    private static LineSnapshot CaptureLine(BufferLine line)
    {
        var runs = new List<RunSnapshot>();
        var cell = new CellData();
        int trimmed = line.GetTrimmedLength();
        int start = 0;
        string fg = string.Empty;
        string bg = string.Empty;
        IReadOnlyList<string> flags = [];
        for (int x = 0; x < trimmed; x++)
        {
            line.LoadCell(x, cell);
            string cellFg = ColorName(cell.GetFgColorMode(), cell.GetFgColor());
            string cellBg = ColorName(cell.GetBgColorMode(), cell.GetBgColor());
            IReadOnlyList<string> cellFlags = FlagNames(cell);
            if (x > 0 && (cellFg != fg || cellBg != bg || !cellFlags.SequenceEqual(flags)))
            {
                runs.Add(new RunSnapshot(start, x - start, fg, bg, flags));
                start = x;
            }

            fg = cellFg;
            bg = cellBg;
            flags = cellFlags;
        }

        if (trimmed > 0)
        {
            runs.Add(new RunSnapshot(start, trimmed - start, fg, bg, flags));
        }

        return new LineSnapshot(line.TranslateToString(true), line.IsWrapped, runs);
    }

    private static string ColorName(uint mode, int value) => mode switch
    {
        Attributes.CmP16 => $"p16:{value}",
        Attributes.CmP256 => $"p256:{value}",
        Attributes.CmRgb => new StringBuilder("rgb:").Append(value.ToString("x6", System.Globalization.CultureInfo.InvariantCulture)).ToString(),
        _ => "default",
    };

    // Order of the phase 2 conventions: bold, dim, italic, underline:<style>, blink, inverse, invisible, strikethrough, overline.
    private static List<string> FlagNames(CellData cell)
    {
        var flags = new List<string>();
        AddIf(flags, cell.IsBold() != 0, "bold");
        AddIf(flags, cell.IsDim() != 0, "dim");
        AddIf(flags, cell.IsItalic() != 0, "italic");
        if (cell.IsUnderline() != 0)
        {
            UnderlineStyle style = cell.HasExtendedAttrs() != 0 && cell.Extended.UnderlineStyle != UnderlineStyle.None
                ? cell.Extended.UnderlineStyle
                : UnderlineStyle.Single;
            flags.Add("underline:" + style.ToString().ToLowerInvariant());
        }

        AddIf(flags, cell.IsBlink() != 0, "blink");
        AddIf(flags, cell.IsInverse() != 0, "inverse");
        AddIf(flags, cell.IsInvisible() != 0, "invisible");
        AddIf(flags, cell.IsStrikethrough() != 0, "strikethrough");
        AddIf(flags, cell.IsOverline() != 0, "overline");
        return flags;
    }

    private static void AddIf(List<string> flags, bool condition, string name)
    {
        if (condition)
        {
            flags.Add(name);
        }
    }
}
