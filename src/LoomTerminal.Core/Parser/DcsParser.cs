// Ported from xtermjs/xterm.js: src/common/parser/DcsParser.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Common;
using LoomTerminal.Core.Input;

namespace LoomTerminal.Core.Parser;

/// <summary>
/// The DCS sub-parser: dispatches the hook, the payload and the unhook of a DCS command to the handlers registered
/// for its identifier. Handlers registered later run first; a handler returning true stops the chain.
/// </summary>
/// <remarks>
/// Threading: not thread-safe; used from the parser thread only. Handlers are synchronous (ADR 0002): the xterm.js
/// stack state used to resume after a <c>Promise</c> is not ported.
/// </remarks>
internal sealed class DcsParser : IDisposable
{
    private static readonly List<IDcsHandler> EmptyHandlers = [];

    private Dictionary<int, List<IDcsHandler>> _handlers = [];
    private List<IDcsHandler> _active = EmptyHandlers;
    private int _ident;
    private DcsFallbackHandler _handlerFb = static (_, _, _) => { };

    /// <summary>Removes every handler and the fallback.</summary>
    public void Dispose()
    {
        _handlers = [];
        _handlerFb = static (_, _, _) => { };
        _active = EmptyHandlers;
    }

    /// <summary>Registers a handler for a DCS identifier. Dispose the result to remove it.</summary>
    public IDisposable RegisterHandler(int ident, IDcsHandler handler)
    {
        if (!_handlers.TryGetValue(ident, out List<IDcsHandler>? handlerList))
        {
            handlerList = [];
            _handlers[ident] = handlerList;
        }

        handlerList.Add(handler);
        return Disposables.Create(() =>
        {
            int handlerIndex = handlerList.IndexOf(handler);
            if (handlerIndex != -1)
            {
                handlerList.RemoveAt(handlerIndex);
            }
        });
    }

    /// <summary>Removes every handler of a DCS identifier.</summary>
    public void ClearHandler(int ident) => _handlers.Remove(ident);

    /// <summary>Sets the handler called for DCS commands that no registered handler takes.</summary>
    public void SetHandlerFallback(DcsFallbackHandler handler) => _handlerFb = handler;

    /// <summary>Resets the parser; handlers of a command still running are told it failed.</summary>
    public void Reset()
    {
        // force cleanup leftover handlers
        if (_active.Count != 0)
        {
            for (int j = _active.Count - 1; j >= 0; --j)
            {
                _active[j].Unhook(false);
            }
        }

        _active = EmptyHandlers;
        _ident = 0;
    }

    /// <summary>Starts a DCS command (always resets leftover handlers). <paramref name="parameters"/> is borrowed.</summary>
    public void Hook(int ident, IParams parameters)
    {
        // always reset leftover handlers
        Reset();
        _ident = ident;
        _active = _handlers.GetValueOrDefault(ident) ?? EmptyHandlers;
        if (_active.Count == 0)
        {
            _handlerFb(_ident, DcsFallbackAction.Hook, parameters);
        }
        else
        {
            for (int j = _active.Count - 1; j >= 0; j--)
            {
                _active[j].Hook(parameters);
            }
        }
    }

    /// <summary>Puts a payload chunk to the current DCS command.</summary>
    public void Put(uint[] data, int start, int end)
    {
        if (_active.Count == 0)
        {
            _handlerFb(_ident, DcsFallbackAction.Put, TextDecoder.Utf32ToString(data, start, end));
        }
        else
        {
            for (int j = _active.Count - 1; j >= 0; j--)
            {
                _active[j].Put(data, start, end);
            }
        }
    }

    /// <summary>Ends the DCS command; <paramref name="success"/> tells whether it finished normally or got aborted.</summary>
    public void Unhook(bool success)
    {
        if (_active.Count == 0)
        {
            _handlerFb(_ident, DcsFallbackAction.Unhook, success);
        }
        else
        {
            int j = _active.Count - 1;
            for (; j >= 0; j--)
            {
                if (_active[j].Unhook(success))
                {
                    break;
                }
            }

            j--;

            // cleanup left over handlers
            for (; j >= 0; j--)
            {
                _active[j].Unhook(false);
            }
        }

        _active = EmptyHandlers;
        _ident = 0;
    }
}
