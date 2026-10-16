// Ported from xtermjs/xterm.js: src/common/parser/DcsParser.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using System.Text;
using LoomTerminal.Core.Input;

namespace LoomTerminal.Core.Parser;

/// <summary>Convenient class to create a DCS handler from a single callback function.</summary>
/// <remarks>
/// Threading: not thread-safe; used from the parser thread only. The payload is limited to
/// <see cref="ParserLimits.PayloadLimit"/> characters and collected in a <see cref="StringBuilder"/> (xterm.js
/// appends to a string): same behavior, linear cost.
/// </remarks>
internal sealed class DcsHandler(Func<string, IParams, bool> handler) : IDcsHandler
{
    // predefine empty params as [0] (ZDM)
    private static readonly Params EmptyParams = CreateEmptyParams();

    private readonly StringBuilder _data = new();
    private IParams _params = EmptyParams;
    private bool _hitLimit;

    /// <inheritdoc />
    public void Hook(IParams p)
    {
        // since the params must be preserved until Unhook, they are cloned (they are only borrowed from the parser
        // and span multiple parser states); perf: clone only non empty params, otherwise stick with the default
        _params = p.Length > 1 || p.ParamValues[0] != 0 ? p.Clone() : EmptyParams;
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
    public bool Unhook(bool success)
    {
        bool ret = false;
        if (_hitLimit)
        {
            ret = false;
        }
        else if (success)
        {
            ret = handler(_data.ToString(), _params);
        }

        _params = EmptyParams;
        _data.Clear();
        _hitLimit = false;
        return ret;
    }

    private static Params CreateEmptyParams()
    {
        Params emptyParams = new();
        emptyParams.AddParam(0);
        return emptyParams;
    }
}
