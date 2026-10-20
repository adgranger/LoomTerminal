# Performance

Reference numbers of the emulator core, measured **before any optimization** (card P2-06). Future optimizations are
compared against this table.

## Machine

- 13th Gen Intel Core i7-13700H (14 cores / 20 threads), 64 GB RAM, Windows 11 (10.0.26300)
- .NET SDK 11.0.100-rc.2, X64 RyuJIT x86-64-v3, BenchmarkDotNet 0.16.0-preview.2, `ShortRun` job (3 iterations)
- Date: 2026-10-21, commit `c4ca107` plus the benchmark project

`ShortRun` is quick but noisy (the Error column of a 3-iteration run is wide): treat the numbers as an order of
magnitude, and re-run with the default job before drawing a conclusion from a small difference.

## Results

All benchmarks write through `TerminalCore.Write(ReadOnlySpan<byte>)`. MB/s = payload (MiB) / mean time.

| Benchmark | Payload | 80x24 mean | 80x24 MB/s | 200x50 mean | 200x50 MB/s | Allocated (80x24) |
|---|---|---:|---:|---:|---:|---:|
| `PlainText_10MB` (79 chars + CRLF) | 10 MiB | 65.8 ms | 152 | 111.6 ms | 90 | 20.7 MB |
| `ColoredText_10MB` (SGR 256 every 8 chars) | 10 MiB | 74.6 ms | 134 | 61.2 ms | 163 | 14.2 MB |
| `Yes_10MB` (`y\r\n`) | 10 MiB | 367.8 ms | 27 | 364.7 ms | 27 | 559 MB |
| `CursorHeavy_1MB` (CUP + text) | 1 MiB | 6.05 ms | 165 | 7.83 ms | 128 | 0 |
| `UnicodeWide_1MB` (CJK + emoji) | 1 MiB | 4.27 ms | 234 | 4.51 ms | 222 | 1.4 MB |
| `RecordedGitLog` (235 KB, 100 replays) | 0.22 MiB | 1.76 ms | 128 | 3.36 ms | 67 | 0.7 MB |

## Observations (no action taken here)

- No number is below the 20 MB/s alert threshold, so no follow-up card was opened.
- `Yes_10MB` is the slowest (27 MB/s) and allocates about 56 bytes per input byte (3.5 million lines, one scroll each):
  the line-feed / scroll path allocates per line. Worth profiling when optimizing.
- `PlainText_10MB` allocates 20 MB for 10 MiB of input; `CursorHeavy_1MB` allocates nothing.

## How to run

```powershell
rtk dotnet run -c Release --project tests/LoomTerminal.Benchmarks -- --filter *
```

The project is not a test project: `dotnet test` does not run it. Results are written to
`BenchmarkDotNet.Artifacts/` in the current folder (not committed).
