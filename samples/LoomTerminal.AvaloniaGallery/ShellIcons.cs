using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace LoomTerminal.AvaloniaGallery;

/// <summary>Loads the icon of a shell executable with the Win32 icon APIs (Native AOT safe).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal static partial class ShellIcons
{
    private const int Size = 32;

    private static readonly Dictionary<string, Bitmap?> Cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Returns the icon of <paramref name="executablePath"/>, or <c>null</c> when it cannot be loaded.</summary>
    public static Bitmap? Load(string? executablePath)
    {
        if (executablePath is null)
        {
            return null;
        }

        if (!Cache.TryGetValue(executablePath, out var bitmap))
        {
            bitmap = LoadCore(IconSource(executablePath));
            Cache[executablePath] = bitmap;
        }

        return bitmap;
    }

    // Git's bin\bash.exe has no icon of its own: Windows Terminal shows the one of git-bash.exe at the Git root.
    private static string IconSource(string path)
    {
        if (Path.GetFileName(path).Equals("bash.exe", StringComparison.OrdinalIgnoreCase)
            && Path.GetDirectoryName(Path.GetDirectoryName(path)) is { } root
            && Path.Combine(root, "git-bash.exe") is var gitBash
            && File.Exists(gitBash))
        {
            return gitBash;
        }

        return path;
    }

    private static WriteableBitmap? LoadCore(string path)
    {
        if (ExtractIconExW(path, 0, out var large, out _, 1) < 1 || large == 0)
        {
            return null;
        }

        try
        {
            return ToBitmap(large);
        }
        finally
        {
            _ = DestroyIcon(large);
        }
    }

    private static WriteableBitmap? ToBitmap(nint icon)
    {
        if (!GetIconInfo(icon, out var info))
        {
            return null;
        }

        try
        {
            // A negative height asks for top-down rows.
            var header = new BitmapInfoHeader { Size = 40, Width = Size, Height = -Size, Planes = 1, BitCount = 32 };
            var pixels = new byte[Size * Size * 4];
            var dc = GetDC(0);
            var rows = GetDIBits(dc, info.ColorBitmap, 0, Size, pixels, ref header, 0);
            _ = ReleaseDC(0, dc);
            if (rows != Size)
            {
                return null;
            }

            // Icons without an alpha channel (old 24-bit ones) come back fully transparent: show them opaque.
            var hasAlpha = false;
            for (var i = 3; i < pixels.Length && !hasAlpha; i += 4)
            {
                hasAlpha = pixels[i] != 0;
            }

            if (!hasAlpha)
            {
                for (var i = 3; i < pixels.Length; i += 4)
                {
                    pixels[i] = 255;
                }
            }

            var bitmap = new WriteableBitmap(
                new PixelSize(Size, Size), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Unpremul);
            using var frame = bitmap.Lock();
            Marshal.Copy(pixels, 0, frame.Address, pixels.Length);
            return bitmap;
        }
        finally
        {
            _ = DeleteObject(info.ColorBitmap);
            _ = DeleteObject(info.MaskBitmap);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IconInfo
    {
        public int IsIcon;
        public int XHotspot;
        public int YHotspot;
        public nint MaskBitmap;
        public nint ColorBitmap;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfoHeader
    {
        public uint Size;
        public int Width;
        public int Height;
        public ushort Planes;
        public ushort BitCount;
        public uint Compression;
        public uint SizeImage;
        public int XPelsPerMeter;
        public int YPelsPerMeter;
        public uint ColorUsed;
        public uint ColorImportant;
    }

    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial uint ExtractIconExW(string file, int index, out nint large, out nint small, uint count);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetIconInfo(nint icon, out IconInfo info);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DestroyIcon(nint icon);

    [LibraryImport("user32.dll")]
    private static partial nint GetDC(nint window);

    [LibraryImport("user32.dll")]
    private static partial int ReleaseDC(nint window, nint dc);

    [LibraryImport("gdi32.dll")]
    private static partial int GetDIBits(nint dc, nint bitmap, uint start, uint lines, byte[] bits, ref BitmapInfoHeader info, uint usage);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DeleteObject(nint obj);
}
