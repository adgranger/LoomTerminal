// Ported from xtermjs/xterm.js: src/common/InputHandler.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Input;
using LoomTerminal.Core.Parser;
using LoomTerminal.Core.Services;

namespace LoomTerminal.Core.InputHandling;

internal partial class InputHandler
{
    private string _windowTitle = string.Empty;
    private string _iconName = string.Empty;

    /// <summary>OSC 0 and OSC 2 (set window title): sets the title and raises <see cref="OnTitleChange"/>.</summary>
    public bool SetTitle(string data)
    {
        _windowTitle = data;
        _onTitleChange.Fire(data);
        return true;
    }

    /// <summary>OSC 1 (set icon name): the icon name is not exposed.</summary>
    public bool SetIconName(string data)
    {
        _iconName = data;
        return true;
    }

    /// <summary>OSC 4 (set ANSI color): sets or reports palette colors, as <c>c ; spec</c> pairs; <c>?</c> asks for the current color.</summary>
    public bool SetOrReportIndexedColor(string data)
    {
        List<ColorRequest> colorEvent = [];
        List<string> slots = [.. data.Split(';')];
        while (slots.Count > 1)
        {
            string idx = slots[0];
            string spec = slots[1];
            slots.RemoveRange(0, 2);
            if (IsDigits(idx) && int.TryParse(idx, out int index) && IsValidColorIndex(index))
            {
                if (spec == "?")
                {
                    colorEvent.Add(new ColorReportRequest(index));
                }
                else
                {
                    (int R, int G, int B)? color = XParseColor.ParseColor(spec);
                    if (color is not null)
                    {
                        colorEvent.Add(new ColorSetRequest(index, color.Value));
                    }
                }
            }
        }

        if (colorEvent.Count > 0)
        {
            _onColor.Fire(colorEvent);
        }

        return true;
    }

    /// <summary>OSC 8 (hyperlink): <c>OSC 8 ; params ; uri</c> opens a hyperlink, an empty uri closes it.</summary>
    public bool SetHyperlink(string data)
    {
        // Arg parsing is special cases to support unencoded semi-colons in the URIs (#4944)
        int idx = data.IndexOf(';', StringComparison.Ordinal);
        if (idx == -1)
        {
            // malformed sequence, just return as handled
            return true;
        }

        string id = data[..idx].Trim();
        string uri = data[(idx + 1)..];
        if (uri.Length > 0)
        {
            return CreateHyperlink(id, uri);
        }

        if (id.Trim().Length > 0)
        {
            return false;
        }

        return FinishHyperlink();
    }

    private bool CreateHyperlink(string parameters, string uri)
    {
        // It's legal to open a new hyperlink without explicitly finishing the previous one
        if (GetCurrentLinkId() != 0)
        {
            FinishHyperlink();
        }

        string[] parsedParams = parameters.Split(':');
        string? id = null;
        int idParamIndex = Array.FindIndex(parsedParams, e => e.StartsWith("id=", StringComparison.Ordinal));
        if (idParamIndex != -1)
        {
            string value = parsedParams[idParamIndex][3..];
            id = value.Length > 0 ? value : null;
        }

        _curAttrData.Extended = _curAttrData.Extended.Clone();
        _curAttrData.Extended.UrlId = _oscLinkService.RegisterLink(new OscLinkData(id, uri));
        _curAttrData.UpdateExtended();
        return true;
    }

    private bool FinishHyperlink()
    {
        _curAttrData.Extended = _curAttrData.Extended.Clone();
        _curAttrData.Extended.UrlId = 0;
        _curAttrData.UpdateExtended();
        return true;
    }

    // special colors - OSC 10 | 11 | 12
    private static readonly int[] SpecialColors = [SpecialColorIndex.Foreground, SpecialColorIndex.Background, SpecialColorIndex.Cursor];

    private bool SetOrReportSpecialColor(string data, int offset)
    {
        string[] slots = data.Split(';');
        for (int i = 0; i < slots.Length; ++i, ++offset)
        {
            if (offset >= SpecialColors.Length)
            {
                break;
            }

            if (slots[i] == "?")
            {
                _onColor.Fire([new ColorReportRequest(SpecialColors[offset])]);
            }
            else
            {
                (int R, int G, int B)? color = XParseColor.ParseColor(slots[i]);
                if (color is not null)
                {
                    _onColor.Fire([new ColorSetRequest(SpecialColors[offset], color.Value)]);
                }
            }
        }

        return true;
    }

    /// <summary>OSC 10: sets or reports the foreground color (and the following special colors).</summary>
    public bool SetOrReportFgColor(string data) => SetOrReportSpecialColor(data, 0);

    /// <summary>OSC 11: sets or reports the background color (and the cursor color).</summary>
    public bool SetOrReportBgColor(string data) => SetOrReportSpecialColor(data, 1);

    /// <summary>OSC 12: sets or reports the cursor color.</summary>
    public bool SetOrReportCursorColor(string data) => SetOrReportSpecialColor(data, 2);

    /// <summary>OSC 104: restores palette colors (the whole palette without data).</summary>
    public bool RestoreIndexedColor(string data)
    {
        if (data.Length == 0)
        {
            _onColor.Fire([new ColorRestoreRequest(null)]);
            return true;
        }

        List<ColorRequest> colorEvent = [];
        string[] slots = data.Split(';');
        for (int i = 0; i < slots.Length; ++i)
        {
            if (IsDigits(slots[i]) && int.TryParse(slots[i], out int index) && IsValidColorIndex(index))
            {
                colorEvent.Add(new ColorRestoreRequest(index));
            }
        }

        if (colorEvent.Count > 0)
        {
            _onColor.Fire(colorEvent);
        }

        return true;
    }

    /// <summary>OSC 110: restores the foreground color.</summary>
    public bool RestoreFgColor(string data)
    {
        _onColor.Fire([new ColorRestoreRequest(SpecialColorIndex.Foreground)]);
        return true;
    }

    /// <summary>OSC 111: restores the background color.</summary>
    public bool RestoreBgColor(string data)
    {
        _onColor.Fire([new ColorRestoreRequest(SpecialColorIndex.Background)]);
        return true;
    }

    /// <summary>OSC 112: restores the cursor color.</summary>
    public bool RestoreCursorColor(string data)
    {
        _onColor.Fire([new ColorRestoreRequest(SpecialColorIndex.Cursor)]);
        return true;
    }

    /// <summary>Whether <paramref name="value"/> is a palette index (0 to 255).</summary>
    internal static bool IsValidColorIndex(int value) => value is >= 0 and < 256;

    // JS: /^\d+$/
    private static bool IsDigits(string value) => value.Length > 0 && value.All(static c => c is >= '0' and <= '9');

    private void RegisterOscHandlers()
    {
        _parser.RegisterOscHandler(0, new OscHandler(data =>
        {
            SetTitle(data);
            SetIconName(data);
            return true;
        }));
        _parser.RegisterOscHandler(1, new OscHandler(SetIconName));
        _parser.RegisterOscHandler(2, new OscHandler(SetTitle));
        _parser.RegisterOscHandler(4, new OscHandler(SetOrReportIndexedColor));
        _parser.RegisterOscHandler(8, new OscHandler(SetHyperlink));
        _parser.RegisterOscHandler(10, new OscHandler(SetOrReportFgColor));
        _parser.RegisterOscHandler(11, new OscHandler(SetOrReportBgColor));
        _parser.RegisterOscHandler(12, new OscHandler(SetOrReportCursorColor));
        _parser.RegisterOscHandler(104, new OscHandler(RestoreIndexedColor));
        _parser.RegisterOscHandler(110, new OscHandler(RestoreFgColor));
        _parser.RegisterOscHandler(111, new OscHandler(RestoreBgColor));
        _parser.RegisterOscHandler(112, new OscHandler(RestoreCursorColor));
    }
}
