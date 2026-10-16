// Ported from xtermjs/xterm.js: src/common/parser/EscapeSequenceParser.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Core.Parser;

internal partial class EscapeSequenceParser
{
    // Pseudo-character placeholder for printable non-ascii characters (unicode).
    private const int NonAsciiPrintable = 0xA0;

    /// <summary>
    /// Parses the UTF-32 code points in <paramref name="data"/> up to <paramref name="length"/>.
    /// </summary>
    /// <remarks>
    /// For several actions with high data load the parsing is optimized by local read ahead loops with hardcoded
    /// conditions to avoid costly table lookups. Any change of table values must be reflected in the loop conditions
    /// as well and vice versa. Affected states/actions: GROUND:PRINT, CSI_PARAM:PARAM, DCS_PARAM:PARAM,
    /// OSC_STRING:OSC_PUT, DCS_PASSTHROUGH:DCS_PUT.
    /// </remarks>
    /// <param name="data">The data to parse.</param>
    /// <param name="length">The number of code points to parse.</param>
    public void Parse(uint[] data, int length)
    {
        uint code;
        int transition = 0;

        // process input string
        for (int i = 0; i < length; ++i)
        {
            code = data[i];

            // normal transition & action lookup
            transition = _transitions.Table[(((int)CurrentState) << TableAccess.IndexStateShift) | (code < 0xa0 ? (int)code : NonAsciiPrintable)];
            switch ((ParserAction)(transition >> TableAccess.TransitionActionShift))
            {
                case ParserAction.Print:
                    // read ahead with loop unrolling
                    // Note: 0x20 (SP) is included, 0x7F (DEL) is excluded
                    for (int j = i + 1; ; ++j)
                    {
                        if (j >= length || (code = data[j]) < 0x20 || (code > 0x7e && code < NonAsciiPrintable))
                        {
                            _printHandler(data, i, j);
                            i = j - 1;
                            break;
                        }

                        if (++j >= length || (code = data[j]) < 0x20 || (code > 0x7e && code < NonAsciiPrintable))
                        {
                            _printHandler(data, i, j);
                            i = j - 1;
                            break;
                        }

                        if (++j >= length || (code = data[j]) < 0x20 || (code > 0x7e && code < NonAsciiPrintable))
                        {
                            _printHandler(data, i, j);
                            i = j - 1;
                            break;
                        }

                        if (++j >= length || (code = data[j]) < 0x20 || (code > 0x7e && code < NonAsciiPrintable))
                        {
                            _printHandler(data, i, j);
                            i = j - 1;
                            break;
                        }
                    }

                    break;
                case ParserAction.Execute:
                    if (_executeHandlers.TryGetValue((int)code, out ExecuteHandler? executeHandler))
                    {
                        executeHandler();
                    }
                    else
                    {
                        _executeHandlerFb((int)code);
                    }

                    PrecedingJoinState = 0;
                    break;
                case ParserAction.Ignore:
                    break;
                case ParserAction.Error:
                    ParsingState inject = _errorHandler(new ParsingState
                    {
                        Position = i,
                        Code = (int)code,
                        CurrentState = CurrentState,
                        Collect = _collect,
                        Params = _params,
                        Abort = false,
                    });
                    if (inject.Abort)
                    {
                        return;
                    }

                    // inject values: currently not implemented
                    break;
                case ParserAction.CsiDispatch:
                    // Trigger CSI Handler
                    int csiIdent = (_collect << 8) | (int)code;
                    _csiHandlers.TryGetValue(csiIdent, out List<CsiHandler>? csiHandlers);
                    int csiPos = csiHandlers is not null ? csiHandlers.Count - 1 : -1;
                    for (; csiPos >= 0; csiPos--)
                    {
                        // true means success and to stop bubbling
                        if (csiHandlers![csiPos](_params))
                        {
                            break;
                        }
                    }

                    if (csiPos < 0)
                    {
                        _csiHandlerFb(csiIdent, _params);
                    }

                    PrecedingJoinState = 0;
                    break;
                case ParserAction.Param:
                    // inner loop: digits (0x30 - 0x39) and ; (0x3b) and : (0x3a)
                    do
                    {
                        switch (code)
                        {
                            case 0x3b:
                                _params.AddParam(0); // ZDM
                                break;
                            case 0x3a:
                                _params.AddSubParam(-1);
                                break;
                            default: // 0x30 - 0x39
                                _params.AddDigit((int)code - 48);
                                break;
                        }
                    }
                    while (++i < length && (code = data[i]) > 0x2f && code < 0x3c);

                    i--;
                    break;
                case ParserAction.Collect:
                    _collect <<= 8;
                    _collect |= (int)code;
                    break;
                case ParserAction.EscDispatch:
                    int escIdent = (_collect << 8) | (int)code;
                    _escHandlers.TryGetValue(escIdent, out List<EscHandler>? escHandlers);
                    int escPos = escHandlers is not null ? escHandlers.Count - 1 : -1;
                    for (; escPos >= 0; escPos--)
                    {
                        // true means success and to stop bubbling
                        if (escHandlers![escPos]())
                        {
                            break;
                        }
                    }

                    if (escPos < 0)
                    {
                        _escHandlerFb(escIdent);
                    }

                    PrecedingJoinState = 0;
                    break;
                case ParserAction.Clear:
                    _params.Reset();
                    _params.AddParam(0); // ZDM
                    _collect = 0;
                    break;
                case ParserAction.DcsHook:
                    _dcsParser.Hook((_collect << 8) | (int)code, _params);
                    break;
                case ParserAction.DcsPut:
                    // inner loop - exit DCS_PUT: 0x18, 0x1a, 0x1b, 0x7f, 0x80 - 0x9f
                    // unhook triggered by: 0x1b, 0x9c (success) and 0x18, 0x1a (abort)
                    for (int j = i + 1; ; ++j)
                    {
                        if (j >= length || (code = data[j]) == 0x18 || code == 0x1a || code == 0x1b || (code > 0x7f && code < NonAsciiPrintable))
                        {
                            _dcsParser.Put(data, i, j);
                            i = j - 1;
                            break;
                        }
                    }

                    break;
                case ParserAction.DcsUnhook:
                    _dcsParser.Unhook(code != 0x18 && code != 0x1a);
                    if (code == 0x1b)
                    {
                        transition |= (int)ParserState.Escape;
                    }

                    _params.Reset();
                    _params.AddParam(0); // ZDM
                    _collect = 0;
                    PrecedingJoinState = 0;
                    break;
                case ParserAction.OscStart:
                    _oscParser.Start();
                    break;
                case ParserAction.OscPut:
                    // inner loop: 0x20 (SP) included, 0x7F (DEL) included
                    for (int j = i + 1; ; j++)
                    {
                        if (j >= length || (code = data[j]) < 0x20 || (code > 0x7f && code < NonAsciiPrintable))
                        {
                            _oscParser.Put(data, i, j);
                            i = j - 1;
                            break;
                        }
                    }

                    break;
                case ParserAction.OscEnd:
                    _oscParser.End(code != 0x18 && code != 0x1a);
                    if (code == 0x1b)
                    {
                        transition |= (int)ParserState.Escape;
                    }

                    _params.Reset();
                    _params.AddParam(0); // ZDM
                    _collect = 0;
                    PrecedingJoinState = 0;
                    break;
            }

            CurrentState = (ParserState)(transition & TableAccess.TransitionStateMask);
        }
    }
}
