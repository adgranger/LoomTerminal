// Ported from xtermjs/xterm.js: src/common/parser/OscParser.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Common;

namespace LoomTerminal.Core.Parser;

/// <summary>
/// The OSC sub-parser: parses the identifier of an OSC command and dispatches the payload to the handlers registered
/// for it. Handlers registered later run first; a handler returning true stops the chain.
/// </summary>
/// <remarks>
/// Threading: not thread-safe; used from the parser thread only. Handlers are synchronous (ADR 0002): the xterm.js
/// stack state used to resume after a <c>Promise</c> is not ported.
/// </remarks>
internal sealed class OscParser : IOscParser
{
    private static readonly List<IOscHandler> EmptyHandlers = [];

    private OscState _state = OscState.Start;
    private List<IOscHandler> _active = EmptyHandlers;
    private int _id = -1;
    private Dictionary<int, List<IOscHandler>> _handlers = [];
    private OscFallbackHandler _handlerFb = static (_, _, _) => { };

    /// <summary>Registers a handler for an OSC identifier. Dispose the result to remove it.</summary>
    public IDisposable RegisterHandler(int ident, IOscHandler handler)
    {
        if (!_handlers.TryGetValue(ident, out List<IOscHandler>? handlerList))
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

    /// <summary>Removes every handler of an OSC identifier.</summary>
    public void ClearHandler(int ident) => _handlers.Remove(ident);

    /// <summary>Sets the handler called for OSC commands that no registered handler takes.</summary>
    public void SetHandlerFallback(OscFallbackHandler handler) => _handlerFb = handler;

    /// <summary>Removes every handler and the fallback.</summary>
    public void Dispose()
    {
        _handlers = [];
        _handlerFb = static (_, _, _) => { };
        _active = EmptyHandlers;
    }

    /// <summary>Resets the parser; handlers that already received a payload are told the command failed.</summary>
    public void Reset()
    {
        // force cleanup handlers if payload was already sent
        if (_state == OscState.Payload)
        {
            for (int j = _active.Count - 1; j >= 0; --j)
            {
                _active[j].End(false);
            }
        }

        _active = EmptyHandlers;
        _id = -1;
        _state = OscState.Start;
    }

    /// <summary>Starts a new OSC command (always resets leftover handlers).</summary>
    public void Start()
    {
        Reset();
        _state = OscState.Id;
    }

    /// <summary>
    /// Puts data to the current OSC command, in the form <c>OSC id ; payload ST/BEL</c>. Payload chunks are not
    /// processed further and are passed directly to the handlers.
    /// </summary>
    public void Put(uint[] data, int start, int end)
    {
        if (_state == OscState.Abort)
        {
            return;
        }

        if (_state == OscState.Id)
        {
            while (start < end)
            {
                uint code = data[start++];
                if (code == 0x3b)
                {
                    _state = OscState.Payload;
                    StartActive();
                    break;
                }

                if (code < 0x30 || 0x39 < code)
                {
                    _state = OscState.Abort;
                    return;
                }

                if (_id == -1)
                {
                    _id = 0;
                }

                _id = (int)(_id * 10 + code - 48);
            }
        }

        if (_state == OscState.Payload && end - start > 0)
        {
            PutActive(data, start, end);
        }
    }

    /// <summary>Indicates the end of an OSC command; <paramref name="success"/> tells whether it finished normally or got aborted.</summary>
    public void End(bool success)
    {
        if (_state == OscState.Start)
        {
            return;
        }

        // do nothing if command was faulty
        if (_state != OscState.Abort)
        {
            // if we are still in ID state and get an early end it means that the command has no payload,
            // thus we still have to announce START and send END right after
            if (_state == OscState.Id)
            {
                StartActive();
            }

            if (_active.Count == 0)
            {
                _handlerFb(_id, OscFallbackAction.End, success);
            }
            else
            {
                int j = _active.Count - 1;
                for (; j >= 0; j--)
                {
                    if (_active[j].End(success))
                    {
                        break;
                    }
                }

                j--;

                // cleanup left over handlers: we always have to call End for proper cleanup,
                // here `success` is used to indicate whether a handler should execute
                for (; j >= 0; j--)
                {
                    _active[j].End(false);
                }
            }
        }

        _active = EmptyHandlers;
        _id = -1;
        _state = OscState.Start;
    }

    private void StartActive()
    {
        _active = _handlers.GetValueOrDefault(_id) ?? EmptyHandlers;
        if (_active.Count == 0)
        {
            _handlerFb(_id, OscFallbackAction.Start, null);
        }
        else
        {
            for (int j = _active.Count - 1; j >= 0; j--)
            {
                _active[j].Start();
            }
        }
    }

    private void PutActive(uint[] data, int start, int end)
    {
        if (_active.Count == 0)
        {
            _handlerFb(_id, OscFallbackAction.Put, Input.TextDecoder.Utf32ToString(data, start, end));
        }
        else
        {
            for (int j = _active.Count - 1; j >= 0; j--)
            {
                _active[j].Put(data, start, end);
            }
        }
    }
}
