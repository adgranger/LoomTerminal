// Ported from xtermjs/xterm.js: src/common/TestUtils.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Services;

namespace LoomTerminal.Core.Tests.TestUtils;

/// <summary>A charset service that does nothing, like the xterm.js mock.</summary>
/// <remarks>Threading: not thread-safe; used from a single test thread.</remarks>
internal sealed class MockCharsetService : ICharsetService
{
    public IReadOnlyDictionary<char, string>? Charset { get; set; }

    public int GLevel => 0;

    public void Reset()
    {
    }

    public void SetgLevel(int g)
    {
    }

    public void SetgCharset(int g, IReadOnlyDictionary<char, string>? charset)
    {
    }
}
