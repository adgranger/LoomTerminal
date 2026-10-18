// Ported from xtermjs/xterm.js: src/common/services/Services.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Common;

namespace LoomTerminal.Core.Services;

/// <summary>The terminal modes and the events that carry data toward the pty.</summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal interface ICoreService
{
    /// <summary>Initially the cursor will not be visible until the first time the terminal is focused.</summary>
    bool IsCursorInitialized { get; set; }

    /// <summary>Whether the cursor is hidden (DECTCEM).</summary>
    bool IsCursorHidden { get; set; }

    /// <summary>The ANSI modes.</summary>
    Modes Modes { get; }

    /// <summary>The DEC private modes.</summary>
    DecPrivateModes DecPrivateModes { get; }

    /// <summary>Raised with the data to send to the pty.</summary>
    IEvent<string> OnData { get; }

    /// <summary>Raised when the data comes from the user.</summary>
    IEvent<EventArgs> OnUserInput { get; }

    /// <summary>Raised with the binary data (a latin-1 string) to send to the pty.</summary>
    IEvent<string> OnBinary { get; }

    /// <summary>Raised when the view should scroll to the bottom.</summary>
    IEvent<EventArgs> OnRequestScrollToBottom { get; }

    /// <summary>Resets the modes (not <see cref="IsCursorInitialized"/>).</summary>
    void Reset();

    /// <summary>
    /// Triggers <see cref="OnData"/>. When <paramref name="wasUserInput"/> is true (the data comes from the user, not
    /// from parsing incoming data) it also requests a scroll to the bottom (if <c>ScrollOnUserInput</c> is set) and
    /// raises <see cref="OnUserInput"/>.
    /// </summary>
    void TriggerDataEvent(string data, bool wasUserInput = false);

    /// <summary>Triggers <see cref="OnBinary"/>.</summary>
    void TriggerBinaryEvent(string data);
}
