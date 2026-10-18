// Ported from xtermjs/xterm.js: src/common/TestUtils.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Services;

namespace LoomTerminal.Core.Tests.TestUtils;

/// <summary>An OSC link service that registers nothing: every link gets the id 1.</summary>
/// <remarks>Threading: not thread-safe; used from a single test thread.</remarks>
internal sealed class MockOscLinkService : IOscLinkService
{
    public int RegisterLink(OscLinkData linkData) => 1;

    public OscLinkData? GetLinkData(int linkId) => null;

    public void AddLineToLink(int linkId, int y)
    {
    }
}
