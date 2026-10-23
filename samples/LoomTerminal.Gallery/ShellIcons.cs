using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace LoomTerminal.Gallery;

/// <summary>Loads the icon of a shell executable through the Windows shell thumbnail cache.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal static class ShellIcons
{
    private static readonly Dictionary<string, Task<ImageSource?>> Cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Returns the icon of <paramref name="executablePath"/>, or <c>null</c> when it cannot be loaded.</summary>
    public static Task<ImageSource?> LoadAsync(string? executablePath)
    {
        if (executablePath is null)
        {
            return Task.FromResult<ImageSource?>(null);
        }

        if (!Cache.TryGetValue(executablePath, out var task))
        {
            task = LoadCoreAsync(executablePath);
            Cache[executablePath] = task;
        }

        return task;
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

    private static async Task<ImageSource?> LoadCoreAsync(string path)
    {
        try
        {
            var file = await StorageFile.GetFileFromPathAsync(IconSource(path));
            using var thumbnail = await file.GetThumbnailAsync(ThumbnailMode.SingleItem, 32);
            var bitmap = new BitmapImage();
            await bitmap.SetSourceAsync(thumbnail);
            return bitmap;
        }
        catch (Exception)
        {
            // The icon is cosmetic: a shell must open without it.
            return null;
        }
    }
}
