using LoomTerminal.Presentation.Theming;

namespace LoomTerminal.Presentation.Session;

/// <summary>Shape of the text cursor.</summary>
public enum TerminalCursorStyle
{
    /// <summary>A filled block.</summary>
    Block,

    /// <summary>An underline.</summary>
    Underline,

    /// <summary>A vertical bar.</summary>
    Bar,
}

/// <summary>Options of a <see cref="TerminalSession"/>.</summary>
/// <remarks>Threading: immutable; any thread may use it.</remarks>
public sealed record TerminalSessionOptions
{
    /// <summary>Number of rows kept in the scrollback buffer.</summary>
    public int Scrollback { get; init; } = 1000;

    /// <summary>Initial cursor style (applications may change it with DECSCUSR).</summary>
    public TerminalCursorStyle CursorStyle { get; init; } = TerminalCursorStyle.Block;

    /// <summary>Whether the cursor blinks.</summary>
    public bool CursorBlink { get; init; }

    /// <summary>Distance between tab stops.</summary>
    public int TabStopWidth { get; init; } = 8;

    /// <summary>Characters that delimit words for word selection.</summary>
    public string WordSeparator { get; init; } = " ()[]{}',\"`";

    /// <summary>
    /// Windows build number of the ConPTY behind the session, used to pick the reflow heuristics; defaults to the
    /// build of the running system on Windows and <see langword="null"/> elsewhere.
    /// </summary>
    public int? WindowsPtyBuildNumber { get; init; } = OperatingSystem.IsWindows() ? Environment.OSVersion.Version.Build : null;

    /// <summary>Whether typing, pasting or sending a key scrolls the viewport to the bottom first.</summary>
    public bool ScrollOnUserInput { get; init; } = true;

    /// <summary>Colors of the terminal; <see langword="null"/> uses the defaults.</summary>
    public TerminalTheme? Theme { get; init; }

    /// <summary>Value of the <c>TERM_PROGRAM</c> environment variable of the shell, used by <see cref="TerminalSession.Start"/>.</summary>
    public string TermProgram { get; init; } = "LoomTerminal";
}
