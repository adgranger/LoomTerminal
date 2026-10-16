using System.Threading.Channels;

namespace LoomTerminal.Pty;

/// <summary>
/// A shell running behind a pseudo terminal: bytes of VT sequences in both directions. The connection knows nothing
/// about the screen except its size; interpreting the bytes is the job of the terminal emulator.
/// </summary>
/// <remarks>Threading: every member can be called from any thread.</remarks>
public interface IPtyConnection : IDisposable
{
    /// <summary>
    /// Output of the shell (UTF-8, VT sequences), in chunks; completes when the shell and its pseudo terminal are gone.
    /// The channel is bounded: a reader that stops reading makes the shell wait (flow control).
    /// </summary>
    ChannelReader<byte[]> Output { get; }

    /// <summary>Completes with the exit code of the shell process.</summary>
    Task<int> Exited { get; }

    /// <summary>Sends input (UTF-8, VT sequences) to the shell. Never blocks; ignored once the connection is disposed.</summary>
    /// <param name="data">The bytes to send.</param>
    void Write(ReadOnlySpan<byte> data);

    /// <summary>Changes the size of the pseudo terminal. Ignored once the shell is gone.</summary>
    /// <param name="columns">Number of columns, at least 1.</param>
    /// <param name="rows">Number of rows, at least 1.</param>
    void Resize(int columns, int rows);
}
