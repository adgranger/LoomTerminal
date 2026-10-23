using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace LoomTerminal.Pty.Unix;

/// <summary>The functions of <c>libloomterm_pty</c> (ADR 0007, <c>src/native/pty</c>).</summary>
/// <remarks>Threading: stateless.</remarks>
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("macos")]
internal static unsafe partial class UnixPtyNative
{
    public const int SigHup = 1;
    public const int SigKill = 9;

    [LibraryImport("loomterm_pty", EntryPoint = "loomterm_spawn", SetLastError = true)]
    public static partial int Spawn(byte* file, byte** argv, byte** envp, byte* cwd, int columns, int rows, out int masterFd, out int pid);

    [LibraryImport("loomterm_pty", EntryPoint = "loomterm_resize", SetLastError = true)]
    public static partial int Resize(int masterFd, int columns, int rows);

    [LibraryImport("loomterm_pty", EntryPoint = "loomterm_wait")]
    public static partial int Wait(int pid);

    [LibraryImport("loomterm_pty", EntryPoint = "loomterm_kill_group", SetLastError = true)]
    public static partial int KillGroup(int pid, int signal);
}
