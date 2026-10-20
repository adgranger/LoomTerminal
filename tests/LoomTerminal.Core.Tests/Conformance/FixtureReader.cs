using System.Globalization;
using System.Text;

namespace LoomTerminal.Core.Tests.Conformance;

/// <summary>One step of a fixture: bytes or text written to the terminal, or a resize.</summary>
internal abstract record FixtureEvent;

/// <summary>Text written to the terminal.</summary>
internal sealed record WriteTextEvent(string Text) : FixtureEvent;

/// <summary>Bytes written to the terminal, as output by a pty.</summary>
internal sealed record WriteBytesEvent(byte[] Bytes) : FixtureEvent;

/// <summary>A terminal resize.</summary>
internal sealed record ResizeEvent(int Cols, int Rows) : FixtureEvent;

/// <summary>A decoded fixture: the initial size and the events to replay.</summary>
internal sealed record FixtureScript(int Cols, int Rows, IReadOnlyList<FixtureEvent> Events);

/// <summary>Reads <c>.input</c> and <c>.vtrec</c> fixtures and replays them into a <see cref="TerminalCore"/>.</summary>
/// <remarks>Threading: stateless.</remarks>
internal static class FixtureReader
{
    private const int DefaultCols = 80;
    private const int DefaultRows = 24;

    /// <summary>
    /// Reads a hand-written <c>.input</c> file: physical newlines only separate lines, the input holds the escapes
    /// <c>\e \a \r \n \t \xHH \uHHHH</c>; <c>#!size</c> (first line) and <c>#!resize</c> are directives, other lines starting with <c>#</c> are comments.
    /// </summary>
    /// <exception cref="FormatException">An unknown escape or directive; the message names the line.</exception>
    internal static FixtureScript ReadInput(string text)
    {
        string[] lines = text.ReplaceLineEndings("\n").Split('\n');
        int cols = DefaultCols;
        int rows = DefaultRows;
        var events = new List<FixtureEvent>();
        var pending = new StringBuilder();
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            int lineNumber = i + 1;
            if (line.StartsWith("#!size ", StringComparison.Ordinal) && i == 0)
            {
                (cols, rows) = ParseSize(line["#!size ".Length..], lineNumber);
            }
            else if (line.StartsWith("#!resize ", StringComparison.Ordinal))
            {
                Flush(pending, events);
                (int newCols, int newRows) = ParseSize(line["#!resize ".Length..], lineNumber);
                events.Add(new ResizeEvent(newCols, newRows));
            }
            else if (line.StartsWith("#!", StringComparison.Ordinal))
            {
                throw new FormatException($"Line {lineNumber}: unknown directive '{line}'.");
            }
            else if (!line.StartsWith('#'))
            {
                DecodeEscapes(line, lineNumber, pending);
            }
        }

        Flush(pending, events);
        return new FixtureScript(cols, rows, events);
    }

    /// <summary>Reads a <c>.vtrec</c> file: <c>O &lt;base64&gt;</c> (pty output) and <c>R &lt;cols&gt; &lt;rows&gt;</c> (resize) per line.</summary>
    /// <exception cref="FormatException">An unknown or malformed event; the message names the line.</exception>
    internal static FixtureScript ReadRecording(string text)
    {
        var events = new List<FixtureEvent>();
        string[] lines = text.ReplaceLineEndings("\n").Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (line.Length == 0)
            {
                continue;
            }

            if (line.StartsWith("O ", StringComparison.Ordinal))
            {
                if (!TryFromBase64(line[2..], out byte[] bytes))
                {
                    throw new FormatException($"Line {i + 1}: invalid base64.");
                }

                events.Add(new WriteBytesEvent(bytes));
            }
            else if (line.StartsWith("R ", StringComparison.Ordinal))
            {
                (int cols, int rows) = ParseSize(line[2..], i + 1);
                events.Add(new ResizeEvent(cols, rows));
            }
            else
            {
                throw new FormatException($"Line {i + 1}: unknown event '{line}'.");
            }
        }

        return new FixtureScript(DefaultCols, DefaultRows, events);
    }

    /// <summary>Replays <paramref name="script"/> into a fresh terminal (optionally with the Windows pty heuristics) and captures the resulting screen.</summary>
    internal static ScreenSnapshot Replay(FixtureScript script, LoomTerminal.Core.Services.WindowsPtyOptions? windowsPty = null)
    {
        using var core = new TerminalCore(new LoomTerminal.Core.Services.TerminalOptions { Cols = script.Cols, Rows = script.Rows, WindowsPty = windowsPty });
        string title = string.Empty;
        using IDisposable subscription = core.OnTitleChange.Subscribe(value => title = value);
        foreach (FixtureEvent fixtureEvent in script.Events)
        {
            switch (fixtureEvent)
            {
                case WriteTextEvent write:
                    core.Write(write.Text);
                    break;
                case WriteBytesEvent write:
                    core.Write(write.Bytes);
                    break;
                case ResizeEvent resize:
                    core.Resize(resize.Cols, resize.Rows);
                    break;
            }
        }

        return ScreenSnapshot.Capture(core, title);
    }

    private static void Flush(StringBuilder pending, List<FixtureEvent> events)
    {
        if (pending.Length > 0)
        {
            events.Add(new WriteTextEvent(pending.ToString()));
            pending.Clear();
        }
    }

    private static (int Cols, int Rows) ParseSize(string text, int lineNumber)
    {
        string[] parts = text.Split(' ');
        if (parts.Length != 2
            || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out int cols)
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int rows))
        {
            throw new FormatException($"Line {lineNumber}: expected '<cols> <rows>', got '{text}'.");
        }

        return (cols, rows);
    }

    private static bool TryFromBase64(string text, out byte[] bytes)
    {
        bytes = new byte[(text.Length * 3 / 4) + 3];
        if (Convert.TryFromBase64String(text, bytes, out int written))
        {
            bytes = bytes[..written];
            return true;
        }

        return false;
    }

    private static void DecodeEscapes(string line, int lineNumber, StringBuilder output)
    {
        int i = 0;
        while (i < line.Length)
        {
            char c = line[i++];
            if (c != '\\')
            {
                output.Append(c);
                continue;
            }

            if (i >= line.Length)
            {
                throw new FormatException($"Line {lineNumber}: dangling backslash.");
            }

            char escape = line[i++];
            switch (escape)
            {
                case 'e':
                    output.Append('\x1b');
                    break;
                case 'a':
                    output.Append('\a');
                    break;
                case 'r':
                    output.Append('\r');
                    break;
                case 'n':
                    output.Append('\n');
                    break;
                case 't':
                    output.Append('\t');
                    break;
                case 'x':
                    output.Append((char)ReadHex(line, ref i, 2, lineNumber));
                    break;
                case 'u':
                    output.Append((char)ReadHex(line, ref i, 4, lineNumber));
                    break;
                default:
                    throw new FormatException($"Line {lineNumber}: unknown escape '\\{escape}'.");
            }
        }
    }

    private static int ReadHex(string line, ref int index, int digits, int lineNumber)
    {
        if (index + digits > line.Length
            || !int.TryParse(line.AsSpan(index, digits), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int value))
        {
            throw new FormatException($"Line {lineNumber}: invalid hexadecimal escape.");
        }

        index += digits;
        return value;
    }
}
