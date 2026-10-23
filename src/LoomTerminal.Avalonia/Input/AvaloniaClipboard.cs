using System.Diagnostics;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using ILoomClipboard = LoomTerminal.Presentation.Input.IClipboard;
using IAvaloniaClipboard = Avalonia.Input.Platform.IClipboard;

namespace LoomTerminal.Avalonia.Input;

/// <summary>The Avalonia clipboard behind <see cref="ILoomClipboard"/>.</summary>
/// <remarks>
/// Threading: the session calls it after awaiting parser-thread work, so every call hops back to the UI thread,
/// which the platform clipboards require.
/// </remarks>
internal sealed class AvaloniaClipboard(Func<IAvaloniaClipboard?> source) : ILoomClipboard
{
    /// <inheritdoc />
    public async Task<string?> GetTextAsync()
    {
        try
        {
            return await Dispatcher.UIThread.InvokeAsync(async () =>
                source() is { } clipboard ? await clipboard.TryGetTextAsync() : null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Clipboard held by another application: nothing is pasted.
            Debug.WriteLine($"LoomTerminal: clipboard read failed: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task SetTextAsync(string text)
    {
        try
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (source() is { } clipboard)
                {
                    await clipboard.SetTextAsync(text);
                }
            });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Debug.WriteLine($"LoomTerminal: clipboard write failed: {ex.Message}");
        }
    }
}
