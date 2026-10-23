using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace LoomTerminal.Pty.Windows;

/// <summary>
/// Resolves a Microsoft Store app execution alias (a 0-byte <c>IO_REPARSE_TAG_APPEXECLINK</c> file in
/// <c>%LOCALAPPDATA%\Microsoft\WindowsApps</c>) to the real executable behind it; .NET does not resolve these.
/// </summary>
/// <remarks>Threading: stateless.</remarks>
[SupportedOSPlatform("windows")]
internal static partial class AppExecutionAlias
{
    private const uint AppExecLinkTag = 0x8000001B;
    private const uint FsctlGetReparsePoint = 0x000900A8;
    private const int MaximumReparseDataBufferSize = 16 * 1024;
    private const uint FileShareAll = 7; // READ | WRITE | DELETE
    private const uint OpenExisting = 3;
    private const uint FlagOpenReparsePointAndBackupSemantics = 0x00200000 | 0x02000000;

    /// <summary>The real file behind <paramref name="path"/>; <paramref name="path"/> itself when it is not an alias or cannot be read.</summary>
    public static string Resolve(string path)
    {
        try
        {
            if (!File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint))
            {
                return path;
            }

            using SafeFileHandle handle = CreateFileW(path, 0, FileShareAll, 0, OpenExisting, FlagOpenReparsePointAndBackupSemantics, 0);
            if (handle.IsInvalid)
            {
                return path;
            }

            byte[] buffer = new byte[MaximumReparseDataBufferSize];
            return DeviceIoControl(handle, FsctlGetReparsePoint, 0, 0, buffer, buffer.Length, out int returned, 0)
                ? ParseTarget(buffer.AsSpan(0, returned)) ?? path
                : path;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return path;
        }
    }

    /// <summary>The target executable stored in a whole <c>REPARSE_DATA_BUFFER</c> of an app execution alias; null when it is another kind or malformed.</summary>
    internal static string? ParseTarget(ReadOnlySpan<byte> data)
    {
        // tag (4), data length (2), reserved (2), then: version (4) = 3 and null-terminated UTF-16 strings.
        if (data.Length < 12
            || BitConverter.ToUInt32(data) != AppExecLinkTag
            || BitConverter.ToUInt32(data[8..]) != 3)
        {
            return null;
        }

        ReadOnlySpan<byte> strings = data[12..];
        string target = string.Empty;
        for (int index = 0; index < 3; index++)
        {
            int end = -1;
            for (int i = 0; i + 1 < strings.Length; i += 2)
            {
                if (strings[i] == 0 && strings[i + 1] == 0)
                {
                    end = i;
                    break;
                }
            }

            if (end < 0)
            {
                return null;
            }

            target = Encoding.Unicode.GetString(strings[..end]);
            strings = strings[(end + 2)..];
        }

        return Path.IsPathFullyQualified(target) ? target : null;
    }

    [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial SafeFileHandle CreateFileW(string fileName, uint desiredAccess, uint shareMode, nint securityAttributes, uint creationDisposition, uint flagsAndAttributes, nint templateFile);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DeviceIoControl(SafeFileHandle device, uint controlCode, nint inBuffer, int inBufferSize, byte[] outBuffer, int outBufferSize, out int bytesReturned, nint overlapped);
}
