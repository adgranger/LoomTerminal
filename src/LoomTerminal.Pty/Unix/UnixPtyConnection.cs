using System.Collections;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Channels;
using Microsoft.Win32.SafeHandles;

namespace LoomTerminal.Pty.Unix;

/// <summary>
/// A shell behind a Unix pseudo terminal, started by the native shim (ADR 0007). The shell is a session leader: on
/// dispose its whole process group is hung up, then killed if it does not leave.
/// </summary>
/// <remarks>
/// Threading: output is read on a dedicated thread into a bounded channel; input is written from a queue, so
/// <see cref="Write"/> never blocks the caller; a third thread waits for the process. Every public member can be
/// called from any thread. Same behavior as <c>ConPtyConnection</c>.
/// </remarks>
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("macos")]
internal sealed class UnixPtyConnection : IPtyConnection
{
    private const int OutputChunkSize = 16 * 1024;
    private const int OutputChunksInFlight = 32;
    private static readonly TimeSpan KillGrace = TimeSpan.FromSeconds(2);

    private readonly int _pid;
    private readonly SafeFileHandle _master;
    private readonly Channel<byte[]> _output = Channel.CreateBounded<byte[]>(
        new BoundedChannelOptions(OutputChunksInFlight) { SingleReader = true, SingleWriter = true, FullMode = BoundedChannelFullMode.Wait });
    private readonly Channel<byte[]> _input = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions { SingleReader = true });
    private readonly TaskCompletionSource<int> _exited = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly CancellationTokenSource _disposed = new();
    private readonly Thread _pump;
    private int _disposeStarted;

    private UnixPtyConnection(int pid, SafeFileHandle master)
    {
        _pid = pid;
        _master = master;
        _pump = new Thread(PumpOutput) { IsBackground = true, Name = "LoomTerminal pty output" };
        _pump.Start();
        _ = Task.Run(PumpInputAsync);
        new Thread(WaitForExit) { IsBackground = true, Name = "LoomTerminal pty wait" }.Start();
    }

    public ChannelReader<byte[]> Output => _output.Reader;

    public Task<int> Exited => _exited.Task;

    /// <summary>Starts <see cref="PtyStartOptions.CommandLine"/> behind a new pseudo terminal.</summary>
    /// <exception cref="IOException">The command line is invalid, the program is not found, or the pty could not be created.</exception>
    /// <exception cref="PlatformNotSupportedException">The native shim is not next to the application.</exception>
    public static unsafe UnixPtyConnection Start(PtyStartOptions options)
    {
        List<string> words;
        try
        {
            words = PosixCommandLine.Split(options.CommandLine);
        }
        catch (FormatException e)
        {
            throw new IOException($"The command line is invalid ({options.CommandLine}): {e.Message}", e);
        }

        if (words.Count == 0)
        {
            throw new IOException("The command line is empty.");
        }

        string file = ResolveExecutable(words[0])
            ?? throw new IOException($"The shell could not be started ({options.CommandLine}): '{words[0]}' was not found or is not executable.");
        string? directory = options.WorkingDirectory;
        if (directory is not null && !Directory.Exists(directory))
        {
            throw new IOException($"The working directory does not exist: {directory}");
        }

        byte* nativeFile = null, nativeCwd = null;
        byte** argv = null, envp = null;
        try
        {
            nativeFile = Utf8(file);
            nativeCwd = directory is null ? null : Utf8(directory);
            argv = Utf8Array(words);
            envp = Utf8Array(BuildEnvironment(options.Environment));
            try
            {
                int result = UnixPtyNative.Spawn(nativeFile, argv, envp, nativeCwd, Math.Max(1, options.Columns), Math.Max(1, options.Rows), out int masterFd, out int pid);
                if (result == 0)
                {
                    return new UnixPtyConnection(pid, new SafeFileHandle(masterFd, ownsHandle: true));
                }
            }
            catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
            {
                throw new PlatformNotSupportedException("The native library libloomterm_pty was not found next to the application (see ADR 0007).", e);
            }

            var error = new Win32Exception(Marshal.GetLastPInvokeError());
            throw new IOException($"The pseudo terminal could not be created: {error.Message}", error);
        }
        finally
        {
            Marshal.FreeCoTaskMem((nint)nativeFile);
            Marshal.FreeCoTaskMem((nint)nativeCwd);
            FreeUtf8Array(argv);
            FreeUtf8Array(envp);
        }
    }

    public void Write(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty || Volatile.Read(ref _disposeStarted) != 0)
        {
            return;
        }

        _input.Writer.TryWrite(data.ToArray());
    }

    public void Resize(int columns, int rows)
    {
        if (Volatile.Read(ref _disposeStarted) == 0 && !_exited.Task.IsCompleted)
        {
            _ = UnixPtyNative.Resize((int)_master.DangerousGetHandle(), Math.Max(1, columns), Math.Max(1, rows));
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposeStarted, 1) != 0)
        {
            return;
        }

        _disposed.Cancel();
        _input.Writer.TryComplete();
        if (!_exited.Task.IsCompleted)
        {
            _ = UnixPtyNative.KillGroup(_pid, UnixPtyNative.SigHup);
        }

        // Waiting for the shell can take a while: off the caller's thread.
        _ = Task.Run(Cleanup);
    }

    private void Cleanup()
    {
        if (!_exited.Task.Wait(KillGrace))
        {
            _ = UnixPtyNative.KillGroup(_pid, UnixPtyNative.SigKill);
            _exited.Task.Wait(KillGrace);
        }

        // The slave side is closed once the group is gone, which ends the pump; the master follows it.
        bool pumpEnded = _pump.Join(TimeSpan.FromSeconds(5));
        _master.Dispose();
        if (pumpEnded)
        {
            _disposed.Dispose();
        }
    }

    private void WaitForExit()
    {
        int code = UnixPtyNative.Wait(_pid);
        _exited.TrySetResult(code);
    }

    private void PumpOutput()
    {
        byte[] buffer = new byte[OutputChunkSize];
        try
        {
            while (true)
            {
                int read;
                try
                {
                    read = RandomAccess.Read(_master, buffer, 0);
                }
                catch (IOException)
                {
                    // EIO on Linux: the child closed its side of the pty.
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                if (read == 0)
                {
                    break;
                }

                if (_disposed.IsCancellationRequested)
                {
                    continue;
                }

                try
                {
                    _output.Writer.WriteAsync(buffer.AsSpan(0, read).ToArray(), _disposed.Token).AsTask().GetAwaiter().GetResult();
                }
                catch (OperationCanceledException)
                {
                    // Disposed while the reader was behind: keep draining.
                }
            }
        }
        finally
        {
            _output.Writer.TryComplete();
        }
    }

    private async Task PumpInputAsync()
    {
        try
        {
            await foreach (byte[] chunk in _input.Reader.ReadAllAsync())
            {
                RandomAccess.Write(_master, chunk, 0);
            }
        }
        catch (Exception e) when (e is IOException or ObjectDisposedException)
        {
            // The pty is gone: nothing left to type into.
        }
    }

    /// <summary>A path with a slash is used as is; a bare name is looked up on PATH.</summary>
    private static string? ResolveExecutable(string name)
    {
        if (name.Contains('/'))
        {
            return IsExecutable(name) ? name : null;
        }

        foreach (string directory in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(':', StringSplitOptions.RemoveEmptyEntries))
        {
            string candidate = Path.Combine(directory, name);
            if (IsExecutable(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static bool IsExecutable(string path) =>
        File.Exists(path) && (File.GetUnixFileMode(path) & (UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute)) != 0;

    /// <summary>Environment of this process, the terminal defaults, then the caller's changes, as <c>NAME=value</c>.</summary>
    private static List<string> BuildEnvironment(IReadOnlyDictionary<string, string?>? changes)
    {
        var variables = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            if (entry.Key is string name)
            {
                variables[name] = entry.Value as string ?? string.Empty;
            }
        }

        variables["TERM"] = "xterm-256color";
        variables["COLORTERM"] = "truecolor";
        foreach ((string name, string? value) in changes ?? new Dictionary<string, string?>())
        {
            if (value is null)
            {
                variables.Remove(name);
            }
            else
            {
                variables[name] = value;
            }
        }

        return variables.Select(pair => $"{pair.Key}={pair.Value}").ToList();
    }

    private static unsafe byte* Utf8(string text) => (byte*)Marshal.StringToCoTaskMemUTF8(text);

    /// <summary>A null-terminated array of null-terminated UTF-8 strings.</summary>
    private static unsafe byte** Utf8Array(List<string> items)
    {
        var array = (byte**)NativeMemory.AllocZeroed((nuint)(items.Count + 1), (nuint)sizeof(byte*));
        for (int i = 0; i < items.Count; i++)
        {
            array[i] = Utf8(items[i]);
        }

        return array;
    }

    private static unsafe void FreeUtf8Array(byte** array)
    {
        if (array is null)
        {
            return;
        }

        for (int i = 0; array[i] is not null; i++)
        {
            Marshal.FreeCoTaskMem((nint)array[i]);
        }

        NativeMemory.Free(array);
    }
}
