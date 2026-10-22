// Ported from xtermjs/xterm.js: src/common/Types.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Presentation.Input;

/// <summary>What the host must do with an evaluated key event.</summary>
internal enum KeyboardResultType
{
    SendKey,
    SelectAll,
    PageUp,
    PageDown,
}

/// <summary>Result of evaluating a key event.</summary>
/// <remarks>Threading: mutable, used by one thread at a time.</remarks>
internal sealed class KeyboardResult
{
    /// <summary>The action to take.</summary>
    internal KeyboardResultType Type { get; set; } = KeyboardResultType.SendKey;

    /// <summary>Whether to cancel event propagation.</summary>
    internal bool Cancel { get; set; }

    /// <summary>The bytes to send, as a string.</summary>
    internal string? Key { get; set; }
}
