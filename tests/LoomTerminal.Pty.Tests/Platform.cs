namespace LoomTerminal.Pty.Tests;

/// <summary>Run-time conditions for <c>[Fact(SkipUnless = ...)]</c>: the Windows tests are skipped elsewhere, never excluded.</summary>
public static class Platform
{
    /// <summary>Whether this is Windows (ConPTY tests, shell locator tests).</summary>
    public static bool IsWindows => OperatingSystem.IsWindows();
}
