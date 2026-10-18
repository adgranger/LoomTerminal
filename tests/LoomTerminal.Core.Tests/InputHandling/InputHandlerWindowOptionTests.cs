// Ported from xtermjs/xterm.js: src/common/InputHandler.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.InputHandling;
using LoomTerminal.Core.Services;

namespace LoomTerminal.Core.Tests.InputHandling;

public sealed class InputHandlerWindowOptionTests : InputHandlerTestBase
{
    private readonly List<string> _stack = [];

    public InputHandlerWindowOptionTests()
    {
        BufferService.Resize(10, 10);
        CoreService.OnData.Subscribe(_stack.Add);
    }

    private void SetWindowOptions(WindowOptions options) =>
        OptionsService.RawOptions = OptionsService.RawOptions with { WindowOptions = options };

    private WindowOptions Current => OptionsService.RawOptions.WindowOptions;

    // xterm.js: 'InputHandler' › 'windowOptions' › 'all should be disabled by default and not report'
    [Fact]
    public void All_window_options_are_disabled_by_default_and_do_not_report()
    {
        InputHandler.Parse("\x1b[14t");
        InputHandler.Parse("\x1b[16t");
        InputHandler.Parse("\x1b[18t");
        InputHandler.Parse("\x1b[20t");
        InputHandler.Parse("\x1b[21t");
        Assert.Empty(_stack);
    }

    // xterm.js: 'InputHandler' › 'windowOptions' › '14 - GetWinSizePixels'
    [Fact]
    public void GetWinSizePixels_requests_a_report_from_the_host_instead_of_answering()
    {
        SetWindowOptions(Current with { GetWinSizePixels = true });
        List<WindowsOptionsReportType> requests = [];
        InputHandler.OnRequestWindowsOptionsReport.Subscribe(requests.Add);
        InputHandler.Parse("\x1b[14t");

        // does not report in test terminal due to missing renderer
        Assert.Empty(_stack);
        Assert.Equal<WindowsOptionsReportType>([WindowsOptionsReportType.GetWinSizePixels], requests);
    }

    // xterm.js: 'InputHandler' › 'windowOptions' › '16 - GetCellSizePixels'
    [Fact]
    public void GetCellSizePixels_requests_a_report_from_the_host_instead_of_answering()
    {
        SetWindowOptions(Current with { GetCellSizePixels = true });
        List<WindowsOptionsReportType> requests = [];
        InputHandler.OnRequestWindowsOptionsReport.Subscribe(requests.Add);
        InputHandler.Parse("\x1b[16t");

        // does not report in test terminal due to missing renderer
        Assert.Empty(_stack);
        Assert.Equal<WindowsOptionsReportType>([WindowsOptionsReportType.GetCellSizePixels], requests);
    }

    // xterm.js: 'InputHandler' › 'windowOptions' › '18 - GetWinSizeChars'
    [Fact]
    public void GetWinSizeChars_reports_the_size_in_characters()
    {
        SetWindowOptions(Current with { GetWinSizeChars = true });
        InputHandler.Parse("\x1b[18t");
        Assert.Equal<string>(["\x1b[8;10;10t"], _stack);
        BufferService.Resize(50, 20);
        InputHandler.Parse("\x1b[18t");
        Assert.Equal<string>(["\x1b[8;10;10t", "\x1b[8;20;50t"], _stack);
    }

    private (List<string> Titles, Action<string> Parse) PrepareTitleStacks()
    {
        SetWindowOptions(Current with { PushTitle = true, PopTitle = true });
        List<string> titles = [];
        InputHandler.OnTitleChange.Subscribe(titles.Add);
        return (titles, InputHandler.Parse);
    }

    // xterm.js: 'InputHandler' › 'windowOptions' › '22/23 - PushTitle/PopTitle'
    [Fact]
    public void PushTitle_and_PopTitle_use_both_stacks()
    {
        (List<string> titles, Action<string> parse) = PrepareTitleStacks();
        parse("\x1b]0;1\x07");
        parse("\x1b[22t");
        parse("\x1b]0;2\x07");
        parse("\x1b[22t");
        parse("\x1b]0;3\x07");
        parse("\x1b[22t");
        Assert.Equal<string>(["1", "2", "3"], InputHandler.WindowTitleStack);
        Assert.Equal<string>(["1", "2", "3"], InputHandler.IconNameStack);
        Assert.Equal<string>(["1", "2", "3"], titles);
        parse("\x1b[23t");
        parse("\x1b[23t");
        parse("\x1b[23t");
        parse("\x1b[23t"); // one more to test "overflow"
        Assert.Empty(InputHandler.WindowTitleStack);
        Assert.Empty(InputHandler.IconNameStack);
        Assert.Equal<string>(["1", "2", "3", "3", "2", "1"], titles);
    }

    // xterm.js: 'InputHandler' › 'windowOptions' › '22/23 - PushTitle/PopTitle with ;1'
    [Fact]
    public void PushTitle_and_PopTitle_with_1_only_use_the_icon_name_stack()
    {
        (List<string> titles, Action<string> parse) = PrepareTitleStacks();
        parse("\x1b]0;1\x07");
        parse("\x1b[22;1t");
        parse("\x1b]0;2\x07");
        parse("\x1b[22;1t");
        parse("\x1b]0;3\x07");
        parse("\x1b[22;1t");
        Assert.Empty(InputHandler.WindowTitleStack);
        Assert.Equal<string>(["1", "2", "3"], InputHandler.IconNameStack);
        Assert.Equal<string>(["1", "2", "3"], titles);
        parse("\x1b[23;1t");
        parse("\x1b[23;1t");
        parse("\x1b[23;1t");
        parse("\x1b[23;1t"); // one more to test "overflow"
        Assert.Empty(InputHandler.WindowTitleStack);
        Assert.Empty(InputHandler.IconNameStack);
        Assert.Equal<string>(["1", "2", "3"], titles);
    }

    // xterm.js: 'InputHandler' › 'windowOptions' › '22/23 - PushTitle/PopTitle with ;2'
    [Fact]
    public void PushTitle_and_PopTitle_with_2_only_use_the_window_title_stack()
    {
        (List<string> titles, Action<string> parse) = PrepareTitleStacks();
        parse("\x1b]0;1\x07");
        parse("\x1b[22;2t");
        parse("\x1b]0;2\x07");
        parse("\x1b[22;2t");
        parse("\x1b]0;3\x07");
        parse("\x1b[22;2t");
        Assert.Equal<string>(["1", "2", "3"], InputHandler.WindowTitleStack);
        Assert.Empty(InputHandler.IconNameStack);
        Assert.Equal<string>(["1", "2", "3"], titles);
        parse("\x1b[23;2t");
        parse("\x1b[23;2t");
        parse("\x1b[23;2t");
        parse("\x1b[23;2t"); // one more to test "overflow"
        Assert.Empty(InputHandler.WindowTitleStack);
        Assert.Empty(InputHandler.IconNameStack);
        Assert.Equal<string>(["1", "2", "3", "3", "2", "1"], titles);
    }

    [Fact]
    public void The_title_stacks_are_limited_to_10_entries()
    {
        (_, Action<string> parse) = PrepareTitleStacks();
        for (int i = 0; i < 12; i++)
        {
            parse($"\x1b]0;{i}\x07\x1b[22t");
        }

        Assert.Equal(10, InputHandler.WindowTitleStack.Count);
        Assert.Equal("2", InputHandler.WindowTitleStack[0]);
    }

    // xterm.js: 'InputHandler' › 'windowOptions' › 'DECCOLM - should only work with "SetWinLines" (24) enabled'
    [Fact]
    public void DECCOLM_only_works_with_SetWinLines_enabled()
    {
        // disabled
        InputHandler.Parse("\x1b[?3l");
        Assert.Equal(10, BufferService.Cols);
        InputHandler.Parse("\x1b[?3h");
        Assert.Equal(10, BufferService.Cols);

        // enabled
        InputHandler.Reset();
        SetWindowOptions(Current with { SetWinLines = true });
        InputHandler.Parse("\x1b[?3l");
        Assert.Equal(80, BufferService.Cols);
        InputHandler.Parse("\x1b[?3h");
        Assert.Equal(132, BufferService.Cols);
    }
}
