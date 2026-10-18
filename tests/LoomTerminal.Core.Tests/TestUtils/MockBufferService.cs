// Ported from xtermjs/xterm.js: src/common/TestUtils.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Services;

namespace LoomTerminal.Core.Tests.TestUtils;

/// <summary>A buffer service with only the size (P1-33 adds the buffers).</summary>
/// <remarks>Threading: not thread-safe; used from a single test thread.</remarks>
internal sealed class MockBufferService(int cols, int rows) : IBufferService
{
    public int Cols { get; private set; } = cols;

    public int Rows { get; private set; } = rows;

    public void Resize(int cols, int rows)
    {
        Cols = cols;
        Rows = rows;
    }
}
