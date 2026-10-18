// Ported from xtermjs/xterm.js: src/common/services/UnicodeService.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Services;

namespace LoomTerminal.Core.Tests.Services;

public sealed class UnicodeServiceTests : IDisposable
{
    private sealed class DummyProvider : IUnicodeVersionProvider
    {
        public string Version => "123";

        public int Wcwidth(int ucs) => 2;

        public int CharProperties(int codepoint, int preceding) =>
            UnicodeService.CreatePropertyValue(0, Wcwidth(codepoint));
    }

    private readonly UnicodeService _us = new();

    // xterm.js starts on version 6; LoomTerminal starts on 11 (P1-22), so the ported tests select 6 first.
    public UnicodeServiceTests() => _us.ActiveVersion = "6";

    public void Dispose() => _us.Dispose();

    // xterm.js: 'unicode provider' › 'default to V6' (LoomTerminal defaults to 11: see UnicodeV11Tests)
    [Fact]
    public void Version_6_is_registered_and_selectable()
    {
        Assert.Equal("6", _us.ActiveVersion);
        Assert.Equal(["6", "11"], _us.Versions);
        _us.ActiveVersion = "6";
        Assert.Equal(5, _us.GetStringCellWidth("hello"));
    }

    // xterm.js: 'unicode provider' › 'activate should throw for unknown version'
    [Fact]
    public void Activating_an_unknown_version_throws()
    {
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => _us.ActiveVersion = "55");
        Assert.Contains("unknown Unicode version \"55\"", error.Message);
    }

    // xterm.js: 'unicode provider' › 'should notify about version change'
    [Fact]
    public void Notifies_about_a_version_change()
    {
        List<string> notes = [];
        _us.OnChange.Subscribe(notes.Add);
        DummyProvider dummyProvider = new();
        _us.Register(dummyProvider);
        _us.ActiveVersion = dummyProvider.Version;
        Assert.Equal([dummyProvider.Version], notes);
    }

    // xterm.js: 'unicode provider' › 'correctly changes provider impl'
    [Fact]
    public void Changes_the_provider_implementation()
    {
        Assert.Equal(5, _us.GetStringCellWidth("hello"));
        DummyProvider dummyProvider = new();
        _us.Register(dummyProvider);
        _us.ActiveVersion = dummyProvider.Version;
        Assert.Equal(2 * 5, _us.GetStringCellWidth("hello"));
    }

    // xterm.js: 'unicode provider' › 'wcwidth V6 emoji test'
    [Fact]
    public void Wcwidth_V6_emoji_test()
    {
        int widthV6 = _us.GetStringCellWidth("🤣🤣🤣🤣🤣🤣🤣🤣🤣🤣");
        Assert.Equal(10, widthV6);
    }
}
