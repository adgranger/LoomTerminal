using LoomTerminal.Core.Buffer;

namespace LoomTerminal.Presentation.Links;

/// <summary>A link found in the buffer.</summary>
/// <param name="Text">The target of the link.</param>
/// <param name="Range">Its cells: 1-based, end inclusive, rows are buffer rows (as xterm.js <c>ILink.range</c>).</param>
/// <remarks>Threading: immutable; any thread.</remarks>
internal readonly record struct DetectedLink(string Text, BufferRange Range);
