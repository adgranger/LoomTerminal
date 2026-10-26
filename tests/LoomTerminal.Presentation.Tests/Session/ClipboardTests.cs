using LoomTerminal.Presentation.Input;
using LoomTerminal.Presentation.Session;

namespace LoomTerminal.Presentation.Tests.Session;

public class ClipboardTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private sealed class FakeClipboard(string? text = null) : IClipboard
    {
        public string? Text { get; private set; } = text;

        public Task<string?> GetTextAsync() => Task.FromResult(Text);

        public Task SetTextAsync(string text)
        {
            Text = text;
            return Task.CompletedTask;
        }
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> condition)
    {
        using var cts = new CancellationTokenSource(Timeout);
        while (!await condition())
        {
            cts.Token.ThrowIfCancellationRequested();
            await Task.Delay(10, cts.Token);
        }
    }

    private static async Task SelectHelloAsync(FakePtyConnection fake, TerminalSession session)
    {
        fake.Emit("hello");
        await WaitUntilAsync(async () => (await session.RunOnParserThreadAsync(core => core.Buffers.Active.Lines.Get(0)!.TranslateToString(true))).StartsWith("hello", StringComparison.Ordinal));
        session.SelectAll();
        await WaitUntilAsync(async () => (await session.GetSelectionTextAsync()).Contains("hello", StringComparison.Ordinal));
    }

    // "ctrl" is the copy and paste modifier of the platform: Ctrl on Windows and Linux, Cmd on macOS (where Ctrl+C is the interrupt).
    private static TerminalKeyEvent Key(int code, string key, bool ctrl, bool shift) =>
        OperatingSystem.IsMacOS()
            ? new(code, key, Ctrl: false, Alt: false, Shift: false, Meta: ctrl, TerminalKeyEventType.KeyDown)
            : new(code, key, ctrl, Alt: false, shift, Meta: false, TerminalKeyEventType.KeyDown);

    [Fact]
    public async Task Ctrl_Shift_C_copies_the_selection_and_keeps_it()
    {
        var fake = new FakePtyConnection();
        var clipboard = new FakeClipboard();
        using var session = TerminalSession.Attach(fake, 80, 24);
        session.Clipboard = clipboard;
        await SelectHelloAsync(fake, session);

        var outcome = session.SendKey(Key(DomKeyCodes.KeyC, "C", ctrl: true, shift: true));

        Assert.Equal(KeyInputOutcome.Copy, outcome);
        await WaitUntilAsync(() => Task.FromResult(clipboard.Text?.Contains("hello", StringComparison.Ordinal) == true));
        Assert.Contains("hello", await session.GetSelectionTextAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Ctrl_C_copies_a_selection_made_while_the_shell_is_silent()
    {
        var fake = new FakePtyConnection();
        var clipboard = new FakeClipboard();
        using var session = TerminalSession.Attach(fake, 80, 24);
        session.Clipboard = clipboard;
        await SelectHelloAsync(fake, session);

        // No output arrives after the selection, as when the user drags the mouse at a prompt.
        var outcome = session.SendKey(Key(DomKeyCodes.KeyC, "c", ctrl: true, shift: false));

        Assert.Equal(KeyInputOutcome.Copy, outcome);
        await WaitUntilAsync(() => Task.FromResult(clipboard.Text?.Contains("hello", StringComparison.Ordinal) == true));
    }

    [Fact]
    public async Task Ctrl_V_pastes_with_bracketed_paste_when_the_mode_is_on()
    {
        var fake = new FakePtyConnection();
        using var session = TerminalSession.Attach(fake, 80, 24);
        session.Clipboard = new FakeClipboard("abc");
        fake.Emit("\u001b[?2004h");
        await WaitUntilAsync(() => session.RunOnParserThreadAsync(core => core.CoreService.DecPrivateModes.BracketedPasteMode));

        var outcome = session.SendKey(Key(DomKeyCodes.KeyV, "v", ctrl: true, shift: false));

        Assert.Equal(KeyInputOutcome.Paste, outcome);
        await WaitUntilAsync(() => Task.FromResult(fake.Input == "\u001b[200~abc\u001b[201~"));
    }

    [Fact]
    public async Task Right_click_with_a_selection_copies_it_and_clears_it()
    {
        var fake = new FakePtyConnection();
        var clipboard = new FakeClipboard("not used");
        using var session = TerminalSession.Attach(fake, 80, 24);
        session.Clipboard = clipboard;
        await SelectHelloAsync(fake, session);

        await session.HandleRightClick();

        Assert.Contains("hello", clipboard.Text, StringComparison.Ordinal);
        Assert.Equal(string.Empty, await session.GetSelectionTextAsync());
        Assert.Equal(string.Empty, fake.Input);
    }

    [Fact]
    public async Task Right_click_without_a_selection_pastes()
    {
        var fake = new FakePtyConnection();
        using var session = TerminalSession.Attach(fake, 80, 24);
        session.Clipboard = new FakeClipboard("abc");

        await session.HandleRightClick();

        await WaitUntilAsync(() => Task.FromResult(fake.Input == "abc"));
    }

    [Fact]
    public async Task Pasting_an_empty_clipboard_does_nothing()
    {
        var fake = new FakePtyConnection();
        using var session = TerminalSession.Attach(fake, 80, 24);
        session.Clipboard = new FakeClipboard("");

        await session.HandleRightClick();
        await session.RunOnParserThreadAsync(_ => 0);

        Assert.Equal(string.Empty, fake.Input);
    }

    [Fact]
    public async Task Right_click_selects_the_word_under_the_pointer_when_the_option_is_on()
    {
        var fake = new FakePtyConnection();
        var clipboard = new FakeClipboard();
        using var session = TerminalSession.Attach(fake, 80, 24, new TerminalSessionOptions { RightClickSelectsWord = true });
        session.Clipboard = clipboard;
        fake.Emit("foo bar");
        await WaitUntilAsync(async () => (await session.RunOnParserThreadAsync(core => core.Buffers.Active.Lines.Get(0)!.TranslateToString(true))).StartsWith("foo bar", StringComparison.Ordinal));

        await session.HandleRightClick(new TerminalPointerEvent(5, 0, 40, 0, PointerButton.Right, PointerAction.Down, false, false, false));

        Assert.Equal("bar", clipboard.Text);
    }
}
