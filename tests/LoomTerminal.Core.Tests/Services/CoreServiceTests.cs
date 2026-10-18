// Ported from xtermjs/xterm.js: src/common/services/CoreService.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Services;
using LoomTerminal.Core.Tests.TestUtils;

namespace LoomTerminal.Core.Tests.Services;

public sealed class CoreServiceTests : IDisposable
{
    private readonly MockBufferService _bufferService = new(80, 30);
    private readonly MockOptionsService _options = new();
    private readonly CoreService _coreService;

    public CoreServiceTests() => _coreService = new CoreService(_bufferService, new MockLogService(), _options);

    public void Dispose()
    {
        _coreService.Dispose();
        _options.Dispose();
        _bufferService.Dispose();
    }

    // xterm.js: 'CoreService' › 'reset' › 'should not affect isCursorInitialized'
    [Fact]
    public void Reset_does_not_affect_IsCursorInitialized()
    {
        _coreService.IsCursorInitialized = true;
        _coreService.Reset();
        Assert.True(_coreService.IsCursorInitialized);
        _coreService.IsCursorInitialized = false;
        _coreService.Reset();
        Assert.False(_coreService.IsCursorInitialized);
    }

    [Fact]
    public void Reset_restores_the_default_modes()
    {
        _coreService.Modes.InsertMode = true;
        _coreService.DecPrivateModes.Wraparound = false;
        _coreService.DecPrivateModes.CursorBlink = true;
        _coreService.Reset();
        Assert.False(_coreService.Modes.InsertMode);
        Assert.True(_coreService.DecPrivateModes.Wraparound);
        Assert.Null(_coreService.DecPrivateModes.CursorBlink);
    }

    [Fact]
    public void TriggerDataEvent_for_user_input_fires_OnUserInput_then_OnData()
    {
        List<string> events = [];
        _coreService.OnUserInput.Subscribe(_ => events.Add("user input"));
        _coreService.OnData.Subscribe(data => events.Add($"data {data}"));
        _coreService.TriggerDataEvent("a", wasUserInput: true);
        Assert.Equal(["user input", "data a"], events);
    }

    [Fact]
    public void TriggerDataEvent_does_not_fire_OnUserInput_for_data_that_is_not_user_input()
    {
        List<string> events = [];
        _coreService.OnUserInput.Subscribe(_ => events.Add("user input"));
        _coreService.OnData.Subscribe(data => events.Add($"data {data}"));
        _coreService.TriggerDataEvent("a");
        Assert.Equal(["data a"], events);
    }

    [Fact]
    public void TriggerDataEvent_and_TriggerBinaryEvent_fire_nothing_when_stdin_is_disabled()
    {
        _options.RawOptions = _options.RawOptions with { DisableStdin = true };
        int fired = 0;
        _coreService.OnUserInput.Subscribe(_ => fired++);
        _coreService.OnData.Subscribe(_ => fired++);
        _coreService.OnBinary.Subscribe(_ => fired++);
        _coreService.TriggerDataEvent("a", wasUserInput: true);
        _coreService.TriggerBinaryEvent("b");
        Assert.Equal(0, fired);
    }

    [Fact]
    public void TriggerBinaryEvent_fires_OnBinary()
    {
        List<string> binary = [];
        _coreService.OnBinary.Subscribe(binary.Add);
        _coreService.TriggerBinaryEvent("ÿ");
        Assert.Equal(["ÿ"], binary);
    }

    [Fact]
    public void TriggerDataEvent_requests_a_scroll_to_the_bottom_only_for_user_input_while_scrolled_up()
    {
        int requests = 0;
        _coreService.OnRequestScrollToBottom.Subscribe(_ => requests++);
        _bufferService.Buffer.YBase = 5;
        _bufferService.Buffer.YDisp = 2;

        _coreService.TriggerDataEvent("a");
        Assert.Equal(0, requests);

        _coreService.TriggerDataEvent("a", wasUserInput: true);
        Assert.Equal(1, requests);

        _options.RawOptions = _options.RawOptions with { ScrollOnUserInput = false };
        _coreService.TriggerDataEvent("a", wasUserInput: true);
        Assert.Equal(1, requests);
    }
}
