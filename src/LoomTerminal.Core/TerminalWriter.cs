// Inspired by xtermjs/xterm.js: src/common/input/WriteBuffer.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using System.Threading.Channels;

namespace LoomTerminal.Core;

/// <summary>
/// The single work loop of the emulator (replaces <c>WriteBuffer</c>): it consumes an ordered queue of pty output
/// chunks and actions, and calls a hook after each batch so that the owner can publish a frame. It is the only code
/// that touches the <see cref="TerminalCore"/> once a session runs.
/// </summary>
/// <remarks>Threading: <see cref="Run"/> is synchronous and runs on the owner's dedicated thread; no locks inside.</remarks>
internal sealed class TerminalWriter(TerminalCore core)
{
    /// <summary>A batch ends once this many bytes of output were written.</summary>
    internal const int MaxBatchBytes = 512 * 1024;

    /// <summary>
    /// Processes the work items until the channel is completed (returns) or <paramref name="cancellationToken"/> is
    /// cancelled (throws <see cref="OperationCanceledException"/>). Exceptions of actions and writes propagate.
    /// </summary>
    /// <param name="work">The ordered queue of work items.</param>
    /// <param name="afterBatch">Called once per batch with the number of output bytes written in it.</param>
    /// <param name="cancellationToken">Cancels the loop.</param>
    public void Run(ChannelReader<TerminalWorkItem> work, Action<int> afterBatch, CancellationToken cancellationToken)
    {
        while (work.WaitToReadAsync(cancellationToken).AsTask().GetAwaiter().GetResult())
        {
            int bytesWritten = 0;
            while (bytesWritten < MaxBatchBytes && work.TryRead(out TerminalWorkItem item))
            {
                if (item.Data is { } data)
                {
                    core.Write(data);
                    bytesWritten += data.Length;
                }
                else
                {
                    item.Action!(core);
                }
            }

            afterBatch(bytesWritten);
        }
    }
}
