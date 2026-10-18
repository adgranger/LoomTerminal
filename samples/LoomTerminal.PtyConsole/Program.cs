using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using LoomTerminal.Pty;

namespace LoomTerminal.PtyConsole;

/// <summary>
/// Pass-through between this console window and a shell behind a pseudo terminal: raw VT input in, VT output out.
/// The console window renders the output, so this checks LoomTerminal.Pty without any emulator.
/// </summary>
[SupportedOSPlatform("windows")]
internal static partial class Program
{
    private const int StdInputHandle = -10;
    private const int StdOutputHandle = -11;
    private const uint EnableProcessedInput = 0x0001;
    private const uint EnableLineInput = 0x0002;
    private const uint EnableEchoInput = 0x0004;
    private const uint EnableVirtualTerminalInput = 0x0200;
    private const uint EnableVirtualTerminalProcessing = 0x0004;

    private static async Task<int> Main(string[] args)
    {
        if (!PseudoTerminal.IsSupported)
        {
            await Console.Error.WriteLineAsync("A pseudo terminal needs Windows 10 1809 or later.");
            return 1;
        }

        // Options come before the command line: --record <file>, --size <cols>x<rows> (fixed size, reproducible recordings).
        string? recordPath = null;
        (int Columns, int Rows)? fixedSize = null;
        int optionCount = 0;
        while (optionCount < args.Length && args[optionCount] is "--record" or "--size")
        {
            if (optionCount + 1 >= args.Length)
            {
                await Console.Error.WriteLineAsync($"{args[optionCount]} needs a value.");
                return 2;
            }

            if (args[optionCount] == "--record")
            {
                recordPath = args[optionCount + 1];
            }
            else if (!TryParseSize(args[optionCount + 1], out (int Columns, int Rows) parsed))
            {
                await Console.Error.WriteLineAsync("--size expects <cols>x<rows>, for example 100x30.");
                return 2;
            }
            else
            {
                fixedSize = parsed;
            }

            optionCount += 2;
        }

        string[] commandArgs = args[optionCount..];
        string commandLine = commandArgs.Length > 0
            ? string.Join(' ', commandArgs)
            : Shells.Detect() is [ShellInfo first, ..] ? first.CommandLine : "cmd.exe";

        nint input = GetStdHandle(StdInputHandle);
        nint output = GetStdHandle(StdOutputHandle);
        GetConsoleMode(input, out uint inputMode);
        GetConsoleMode(output, out uint outputMode);
        // Raw VT input: Ctrl+C, arrows, etc. reach the shell as bytes instead of being handled by this console.
        SetConsoleMode(input, (inputMode | EnableVirtualTerminalInput) & ~(EnableProcessedInput | EnableLineInput | EnableEchoInput));
        SetConsoleMode(output, outputMode | EnableVirtualTerminalProcessing);
        try
        {
            (int Columns, int Rows) startSize = fixedSize ?? ConsoleSize();
            using SessionRecorder? recorder = recordPath is null ? null : new SessionRecorder(recordPath);
            recorder?.RecordResize(startSize.Columns, startSize.Rows);
            using IPtyConnection pty = PseudoTerminal.Start(new PtyStartOptions(commandLine)
            {
                Columns = startSize.Columns,
                Rows = startSize.Rows,
                Environment = new Dictionary<string, string?> { ["TERM_PROGRAM"] = "LoomTerminal.PtyConsole" },
            });

            using var stop = new CancellationTokenSource();
            Task outputTask = CopyOutputAsync(pty, recorder, stop.Token);
            _ = Task.Run(() => CopyInput(pty), stop.Token);
            if (fixedSize is null)
            {
                _ = WatchSizeAsync(pty, recorder, stop.Token);
            }

            int exitCode = await pty.Exited;
            await outputTask;
            await stop.CancelAsync();
            return exitCode;
        }
        finally
        {
            SetConsoleMode(input, inputMode);
            SetConsoleMode(output, outputMode);
        }
    }

    private static async Task CopyOutputAsync(IPtyConnection pty, SessionRecorder? recorder, CancellationToken cancellationToken)
    {
        using Stream stdout = Console.OpenStandardOutput();
        await foreach (byte[] chunk in pty.Output.ReadAllAsync(cancellationToken))
        {
            recorder?.RecordOutput(chunk);
            await stdout.WriteAsync(chunk, cancellationToken);
            await stdout.FlushAsync(cancellationToken);
        }
    }

    private static void CopyInput(IPtyConnection pty)
    {
        using Stream stdin = Console.OpenStandardInput();
        byte[] buffer = new byte[4096];
        int read;
        while ((read = stdin.Read(buffer)) > 0)
        {
            pty.Write(buffer.AsSpan(0, read));
        }
    }

    private static async Task WatchSizeAsync(IPtyConnection pty, SessionRecorder? recorder, CancellationToken cancellationToken)
    {
        (int Columns, int Rows) size = ConsoleSize();
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(200));
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                (int Columns, int Rows) current = ConsoleSize();
                if (current != size)
                {
                    size = current;
                    recorder?.RecordResize(current.Columns, current.Rows);
                    pty.Resize(current.Columns, current.Rows);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // The shell is gone.
        }
    }

    private static bool TryParseSize(string text, out (int Columns, int Rows) size)
    {
        size = default;
        string[] parts = text.Split('x');
        if (parts.Length == 2
            && int.TryParse(parts[0], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int columns)
            && int.TryParse(parts[1], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int rows)
            && columns > 0 && rows > 0)
        {
            size = (columns, rows);
            return true;
        }

        return false;
    }

    /// <summary>Size of the console window; 80x24 when there is none (output redirected, as on CI).</summary>
    private static (int Columns, int Rows) ConsoleSize()
    {
        try
        {
            return (Math.Max(Console.WindowWidth, 1), Math.Max(Console.WindowHeight, 1));
        }
        catch (IOException)
        {
            return (80, 24);
        }
    }

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial nint GetStdHandle(int handle);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetConsoleMode(nint handle, out uint mode);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetConsoleMode(nint handle, uint mode);
}
