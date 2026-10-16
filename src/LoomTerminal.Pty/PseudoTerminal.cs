using LoomTerminal.Pty.Windows;

namespace LoomTerminal.Pty;

/// <summary>Starts shells behind the pseudo terminal of the current operating system.</summary>
/// <remarks>Threading: every member can be called from any thread.</remarks>
public static class PseudoTerminal
{
    /// <summary>Whether this operating system has a supported pseudo terminal (Windows 10 1809+ for now).</summary>
    public static bool IsSupported => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763);

    /// <summary>Starts <see cref="PtyStartOptions.CommandLine"/> behind a new pseudo terminal.</summary>
    /// <param name="options">What to start.</param>
    /// <remarks>Ported from VIGO's ConPTY session; macOS and Linux (openpty) are planned for the Avalonia backend.</remarks>
    /// <returns>The running connection; dispose it to kill the shell and every process it started.</returns>
    /// <exception cref="IOException">The pseudo terminal or the process could not be created.</exception>
    /// <exception cref="PlatformNotSupportedException"><see cref="IsSupported"/> is <see langword="false"/>.</exception>
    public static IPtyConnection Start(PtyStartOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.CommandLine);

        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
        {
            return ConPtyConnection.Start(options);
        }

        throw new PlatformNotSupportedException("LoomTerminal.Pty only supports Windows (ConPTY) for now.");
    }
}
