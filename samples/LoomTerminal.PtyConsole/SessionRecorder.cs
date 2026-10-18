using System.Globalization;

namespace LoomTerminal.PtyConsole;

/// <summary>
/// Writes a pty session as a <c>.vtrec</c> file: one <c>O &lt;base64&gt;</c> line per chunk of output and one
/// <c>R &lt;cols&gt; &lt;rows&gt;</c> line per resize (see <c>docs/tasks/phase-2/README.md</c>).
/// </summary>
/// <remarks>Threading: thread-safe; output and resizes may be recorded from different threads.</remarks>
internal sealed class SessionRecorder : IDisposable
{
    private readonly object _gate = new();
    private readonly StreamWriter _writer;

    public SessionRecorder(string path)
    {
        _writer = new StreamWriter(path, append: false, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
        {
            NewLine = "\n",
        };
    }

    /// <summary>Records a chunk of output of the pty.</summary>
    public void RecordOutput(ReadOnlySpan<byte> data)
    {
        string line = "O " + Convert.ToBase64String(data);
        lock (_gate)
        {
            _writer.WriteLine(line);
        }
    }

    /// <summary>Records a resize of the pty.</summary>
    public void RecordResize(int columns, int rows)
    {
        string line = string.Create(CultureInfo.InvariantCulture, $"R {columns} {rows}");
        lock (_gate)
        {
            _writer.WriteLine(line);
        }
    }

    /// <summary>Flushes and closes the file.</summary>
    public void Dispose()
    {
        lock (_gate)
        {
            _writer.Dispose();
        }
    }
}
