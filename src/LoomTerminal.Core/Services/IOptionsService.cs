// Ported from xtermjs/xterm.js: src/common/services/Services.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Common;

namespace LoomTerminal.Core.Services;

/// <summary>Holds the terminal options, validates changes and notifies listeners.</summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal interface IOptionsService
{
    /// <summary>The current, validated options.</summary>
    TerminalOptions RawOptions { get; }

    /// <summary>The options as exposed to the public API (the same object as <see cref="RawOptions"/>).</summary>
    TerminalOptions Options => RawOptions;

    /// <summary>Raised for every option whose value changed, after the new options are in place.</summary>
    IEvent<TerminalOption> OnOptionChange { get; }

    /// <summary>
    /// Validates <paramref name="value"/> and replaces the options. Nothing changes if validation throws. Raises
    /// <see cref="OnOptionChange"/> once per changed property, in <see cref="TerminalOption"/> order.
    /// </summary>
    void SetOptions(TerminalOptions value);

    /// <summary>Adds a listener called when <paramref name="key"/> changes; read the new value from <see cref="RawOptions"/>.</summary>
    IDisposable OnSpecificOptionChange(TerminalOption key, Action listener);

    /// <summary>Adds a listener called when any of <paramref name="keys"/> changes.</summary>
    IDisposable OnMultipleOptionChange(IReadOnlyList<TerminalOption> keys, Action listener);
}
