using System.Text;
using System.Threading.Channels;
using LoomTerminal.Pty;

namespace LoomTerminal.Presentation.Tests.Session;

/// <summary>An <see cref="IPtyConnection"/> over two channels: the test plays the shell.</summary>
internal sealed class FakePtyConnection : IPtyConnection
{
    private readonly Channel<byte[]> _output = Channel.CreateUnbounded<byte[]>();
    private readonly TaskCompletionSource<int> _exited = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly StringBuilder _input = new();
    private readonly Lock _lock = new();

    public ChannelReader<byte[]> Output => _output.Reader;

    public Task<int> Exited => _exited.Task;

    public (int Columns, int Rows)? LastResize { get; private set; }

    /// <summary>Everything the terminal wrote to the "shell", decoded as UTF-8.</summary>
    public string Input
    {
        get
        {
            lock (_lock)
            {
                return _input.ToString();
            }
        }
    }

    public void Write(ReadOnlySpan<byte> data)
    {
        lock (_lock)
        {
            _input.Append(Encoding.UTF8.GetString(data));
        }
    }

    public void Resize(int columns, int rows) => LastResize = (columns, rows);

    /// <summary>Plays shell output.</summary>
    public void Emit(string text) => _output.Writer.TryWrite(Encoding.UTF8.GetBytes(text));

    /// <summary>Ends the output and sets the exit code.</summary>
    public void Exit(int exitCode)
    {
        _output.Writer.TryComplete();
        _exited.TrySetResult(exitCode);
    }

    public void Dispose() => _output.Writer.TryComplete();
}
