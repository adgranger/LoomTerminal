using LoomTerminal.Core;

namespace LoomTerminal.Presentation;

/// <summary>Creates emulator instances through the internal Core API.</summary>
/// <remarks>Threading: stateless; any thread may call it.</remarks>
internal static class CoreFactory
{
    /// <summary>Creates a terminal with default options and returns its size in columns.</summary>
    internal static int CreateAndGetColumns() => new TerminalCore().Cols;
}
