// Ported from xtermjs/xterm.js: src/common/parser/OscParser.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using System.Text;
using LoomTerminal.Core.Input;

namespace LoomTerminal.Core.Parser;

/// <summary>Convenient class to attach a string based function as OSC handler.</summary>
/// <remarks>
/// Threading: not thread-safe; used from the parser thread only. The payload is collected in a
/// <see cref="StringBuilder"/> (xterm.js appends to a string): same behavior, linear cost.
/// </remarks>
internal sealed class OscHandler(Func<string, bool> handler) : IOscHandler
{
    private readonly StringBuilder _data = new();
    private bool _hitLimit;

    /// <inheritdoc />
    public void Start()
    {
        _data.Clear();
        _hitLimit = false;
    }

    /// <inheritdoc />
    public void Put(uint[] data, int start, int end)
    {
        if (_hitLimit)
        {
            return;
        }

        _data.Append(TextDecoder.Utf32ToString(data, start, end));
        if (_data.Length > ParserLimits.PayloadLimit)
        {
            _data.Clear();
            _hitLimit = true;
        }
    }

    /// <inheritdoc />
    public bool End(bool success)
    {
        bool ret = false;
        if (_hitLimit)
        {
            ret = false;
        }
        else if (success)
        {
            ret = handler(_data.ToString());
        }

        _data.Clear();
        _hitLimit = false;
        return ret;
    }
}
