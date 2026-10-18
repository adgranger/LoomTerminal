// Ported from xtermjs/xterm.js: src/common/TestUtils.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Common;
using LoomTerminal.Core.Input;
using LoomTerminal.Core.Services;

namespace LoomTerminal.Core.Tests.TestUtils;

/// <summary>A Unicode service that defaults to V6 always, to keep the tests passing (as the xterm.js mock).</summary>
/// <remarks>Threading: not thread-safe; used from a single test thread.</remarks>
internal sealed class MockUnicodeService : IUnicodeService, IDisposable
{
    private readonly UnicodeV6 _provider = new();
    private readonly Emitter<string> _onChange = new();

    public string[] Versions { get; } = [];

    public string ActiveVersion { get; set; } = string.Empty;

    public void Dispose() => _onChange.Dispose();

    public IEvent<string> OnChange => _onChange.Event;

    public void Register(IUnicodeVersionProvider provider) => throw new NotImplementedException();

    public int Wcwidth(int codepoint) => _provider.Wcwidth(codepoint);

    public int CharProperties(int codepoint, int preceding)
    {
        int width = Wcwidth(codepoint);
        bool shouldJoin = width == 0 && preceding != 0;
        if (shouldJoin)
        {
            int oldWidth = UnicodeService.ExtractWidth(preceding);
            if (oldWidth == 0)
            {
                shouldJoin = false;
            }
            else if (oldWidth > width)
            {
                width = oldWidth;
            }
        }

        return UnicodeService.CreatePropertyValue(0, width, shouldJoin);
    }

    public int GetStringCellWidth(string s) => throw new NotImplementedException();
}
