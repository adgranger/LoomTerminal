// Ported from xtermjs/xterm.js: src/common/services/Services.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Core.Services;

/// <summary>The data of an OSC 8 hyperlink.</summary>
/// <param name="Id">The <c>id=</c> parameter of the link; <see langword="null"/> when absent.</param>
/// <param name="Uri">The target of the link.</param>
internal readonly record struct OscLinkData(string? Id, string Uri);

/// <summary>The registry of OSC 8 hyperlinks.</summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal interface IOscLinkService
{
    /// <summary>
    /// Registers a link to the service, returning the link ID. The link data is managed by this service and will be
    /// freed when this current cursor position is trimmed off the buffer.
    /// </summary>
    int RegisterLink(OscLinkData linkData);

    /// <summary>Adds a line to a link if needed.</summary>
    void AddLineToLink(int linkId, int y);

    /// <summary>Gets the link data associated with a link ID.</summary>
    OscLinkData? GetLinkData(int linkId);
}
