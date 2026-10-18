// Ported from xtermjs/xterm.js: src/common/InputHandler.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Parser;

namespace LoomTerminal.Core.InputHandling;

internal partial class InputHandler
{
    /// <summary>
    /// Registers a CSI handler on the parser. Handlers of <c>CSI Ps t</c> (XTWINOPS) are only called when the
    /// matching <c>WindowOptions</c> option is enabled.
    /// </summary>
    public IDisposable RegisterCsiHandler(FunctionIdentifier id, CsiHandler callback)
    {
        if (id.Final == "t" && id.Prefix is null && id.Intermediates is null)
        {
            // security: always check whether window option is allowed
            return _parser.RegisterCsiHandler(id, p =>
                !ParamToWindowOption(p.ParamValues[0], _optionsService.RawOptions.WindowOptions) || callback(p));
        }

        return _parser.RegisterCsiHandler(id, callback);
    }

    /// <summary>Registers a DCS handler on the parser.</summary>
    public IDisposable RegisterDcsHandler(FunctionIdentifier id, Func<string, IParams, bool> callback) =>
        _parser.RegisterDcsHandler(id, new DcsHandler(callback));

    /// <summary>Registers an ESC handler on the parser.</summary>
    public IDisposable RegisterEscHandler(FunctionIdentifier id, EscHandler callback) =>
        _parser.RegisterEscHandler(id, callback);

    /// <summary>Registers an OSC handler on the parser.</summary>
    public IDisposable RegisterOscHandler(int ident, Func<string, bool> callback) =>
        _parser.RegisterOscHandler(ident, new OscHandler(callback));
}
