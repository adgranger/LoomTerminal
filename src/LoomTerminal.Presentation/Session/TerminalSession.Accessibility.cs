using System.Text;

namespace LoomTerminal.Presentation.Session;

public sealed partial class TerminalSession
{
    // Ported from AccessibilityManager.ts: MAX_ROWS_TO_READ.
    private const int MaxRowsToRead = 20;
    private const string TooMuchOutput = "Too much output to announce, navigate to rows manually to read";

    private readonly Queue<char> _charsToConsume = new();
    private readonly StringBuilder _charsToAnnounce = new();
    private volatile bool _accessibilityEnabled;
    private int _liveRegionLineCount;

    /// <summary>Raised on the parser thread, after a batch of output, with the text printed since the previous announcement.</summary>
    public event Action<string>? TextAnnounced;

    /// <summary>
    /// Whether the session serves a screen reader: the emulator reports printed characters, <see cref="TextAnnounced"/>
    /// is raised and frames carry <see cref="Rendering.RenderFrame.PlainLines"/>. Off by default.
    /// </summary>
    public bool AccessibilityEnabled
    {
        get => _accessibilityEnabled;
        set
        {
            _accessibilityEnabled = value;
            Post(core =>
            {
                core.OptionsService.SetOptions(core.OptionsService.RawOptions with { ScreenReaderMode = value });
                ClearLiveRegion();
            });
        }
    }

    private void InitAccessibility()
    {
        _core.OnA11yChar.Subscribe(HandleChar);
        _core.OnLineFeed.Subscribe(_ => HandleChar("\n"));
        _core.OnA11yTab.Subscribe(count =>
        {
            for (var i = 0; i < count; i++)
            {
                HandleChar(" ");
            }
        });
    }

    private void HandleChar(string text)
    {
        if (!_accessibilityEnabled || _liveRegionLineCount >= MaxRowsToRead + 1)
        {
            return;
        }

        // A character that was just typed is already announced by the input control: skip its echo.
        if (_charsToConsume.Count == 0 || _charsToConsume.Dequeue().ToString() != text)
        {
            _charsToAnnounce.Append(text);
        }

        if (text == "\n" && ++_liveRegionLineCount == MaxRowsToRead + 1)
        {
            _charsToAnnounce.Append(TooMuchOutput);
        }
    }

    // A keypress clears the live region and remembers the printable characters, as AccessibilityManager._handleKey.
    private void NoteTypedText(string text)
    {
        ClearLiveRegion();
        foreach (var c in text)
        {
            if (!char.IsControl(c))
            {
                _charsToConsume.Enqueue(c);
            }
        }
    }

    private void ClearLiveRegion()
    {
        _liveRegionLineCount = 0;
        _charsToAnnounce.Clear();
    }

    private void AnnounceCharacters()
    {
        if (_charsToAnnounce.Length == 0)
        {
            return;
        }

        var text = _charsToAnnounce.ToString().TrimEnd('\n');
        _charsToAnnounce.Clear();
        if (text.Length > 0)
        {
            TextAnnounced?.Invoke(text);
        }
    }
}
