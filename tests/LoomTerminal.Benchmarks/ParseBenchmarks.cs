using System.Text;
using BenchmarkDotNet.Attributes;
using LoomTerminal.Core;
using LoomTerminal.Core.Services;

namespace LoomTerminal.Benchmarks;

/// <summary>
/// Throughput of <see cref="TerminalCore.Write(ReadOnlySpan{byte})"/> on representative inputs, at 80x24 and 200x50.
/// MB/s = payload size / mean time (see docs/performance.md).
/// </summary>
/// <remarks>Threading: BenchmarkDotNet runs every benchmark on one thread.</remarks>
[MemoryDiagnoser]
[ShortRunJob]
public class ParseBenchmarks : IDisposable
{
    private const int TenMegabytes = 10 * 1024 * 1024;
    private const int OneMegabyte = 1024 * 1024;

    private byte[] _plainText = [];
    private byte[] _coloredText = [];
    private byte[] _yes = [];
    private byte[] _cursorHeavy = [];
    private byte[] _unicodeWide = [];
    private byte[] _gitLog = [];
    private TerminalCore _core = null!;

    /// <summary>Terminal size: 80x24 or 200x50.</summary>
    [Params("80x24", "200x50")]
    public string Size { get; set; } = "80x24";

    /// <summary>Builds the inputs once and a fresh terminal of <see cref="Size"/>.</summary>
    [GlobalSetup]
    public void Setup()
    {
        string[] size = Size.Split('x');
        int cols = int.Parse(size[0], System.Globalization.CultureInfo.InvariantCulture);
        int rows = int.Parse(size[1], System.Globalization.CultureInfo.InvariantCulture);
        _core = new TerminalCore(new TerminalOptions { Cols = cols, Rows = rows });

        _plainText = Repeat(new string('a', 79) + "\r\n", TenMegabytes);
        _coloredText = Repeat("\x1b[38;5;196mabcdefg\x1b[38;5;21mhijklmn\x1b[0m ", TenMegabytes);
        _yes = Repeat("y\r\n", TenMegabytes);
        _unicodeWide = Repeat("日本語のテキスト😀🎉한국어中文 ", OneMegabyte);

        var cursor = new StringBuilder();
        for (int i = 0; cursor.Length < OneMegabyte; i++)
        {
            cursor.Append(System.Globalization.CultureInfo.InvariantCulture, $"\x1b[{(i % rows) + 1};{(i * 7 % cols) + 1}Hcell{i % 100}");
        }

        _cursorHeavy = Encoding.UTF8.GetBytes(cursor.ToString());
        _gitLog = ReadRecordedOutput(Path.Combine(AppContext.BaseDirectory, "git-log-graph.vtrec"));
    }

    /// <summary>Releases the terminal.</summary>
    [GlobalCleanup]
    public void Cleanup() => Dispose();

    /// <inheritdoc />
    public void Dispose()
    {
        _core?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>10 MB of ASCII lines of 79 characters + CRLF.</summary>
    [Benchmark]
    public void PlainText_10MB() => _core.Write(_plainText);

    /// <summary>10 MB with a 256-color SGR every 8 characters.</summary>
    [Benchmark]
    public void ColoredText_10MB() => _core.Write(_coloredText);

    /// <summary>10 MB of <c>y\r\n</c>, like the <c>yes</c> command.</summary>
    [Benchmark]
    public void Yes_10MB() => _core.Write(_yes);

    /// <summary>1 MB of CUP + text, like a TUI redraw.</summary>
    [Benchmark]
    public void CursorHeavy_1MB() => _core.Write(_cursorHeavy);

    /// <summary>1 MB of CJK and emoji.</summary>
    [Benchmark]
    public void UnicodeWide_1MB() => _core.Write(_unicodeWide);

    /// <summary>The recorded <c>git log --graph</c> output replayed 100 times.</summary>
    [Benchmark]
    public void RecordedGitLog()
    {
        for (int i = 0; i < 100; i++)
        {
            _core.Write(_gitLog);
        }
    }

    private static byte[] Repeat(string unit, int totalBytes)
    {
        byte[] unitBytes = Encoding.UTF8.GetBytes(unit);
        byte[] result = new byte[totalBytes / unitBytes.Length * unitBytes.Length];
        for (int offset = 0; offset < result.Length; offset += unitBytes.Length)
        {
            unitBytes.CopyTo(result, offset);
        }

        return result;
    }

    private static byte[] ReadRecordedOutput(string path)
    {
        using var output = new MemoryStream();
        foreach (string line in File.ReadLines(path))
        {
            if (line.StartsWith("O ", StringComparison.Ordinal))
            {
                output.Write(Convert.FromBase64String(line[2..]));
            }
        }

        return output.ToArray();
    }
}
