// Inspired by VIGO: src/Vigo.App/Controls/TerminalView.cs (Copy, PasteAsync)
using System.Diagnostics;
using System.Runtime.InteropServices;
using LoomTerminal.Presentation.Input;
using Microsoft.UI.Dispatching;
using Windows.ApplicationModel.DataTransfer;

namespace LoomTerminal.WinUI.Input;

/// <summary>The WinRT clipboard behind <see cref="IClipboard"/>.</summary>
/// <remarks>
/// Threading: create it on the UI thread. The session calls it after awaiting parser-thread work, so every call hops
/// back to the UI thread, which the WinRT clipboard requires.
/// </remarks>
internal sealed class WinRtClipboard : IClipboard
{
    private readonly DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread();

    private Task<T> OnUiThread<T>(Func<Task<T>> action)
    {
        if (_dispatcher.HasThreadAccess)
        {
            return action();
        }

        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_dispatcher.TryEnqueue(async () =>
            {
                try
                {
                    completion.SetResult(await action());
                }
                catch (Exception ex)
                {
                    completion.SetException(ex);
                }
            }))
        {
            completion.SetResult(default!); // the window is closing: nothing to read or write
        }

        return completion.Task;
    }

    /// <inheritdoc />
    public Task<string?> GetTextAsync() => OnUiThread(GetTextCoreAsync);

    private static async Task<string?> GetTextCoreAsync()
    {
        try
        {
            var content = Clipboard.GetContent();
            return content.Contains(StandardDataFormats.Text) ? await content.GetTextAsync() : null;
        }
        catch (Exception ex) when (ex is COMException or InvalidOperationException)
        {
            // Clipboard held by another application: nothing is pasted.
            Debug.WriteLine($"LoomTerminal: clipboard read failed: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc />
    public Task SetTextAsync(string text) => OnUiThread(() => SetTextCore(text));

    private static Task<bool> SetTextCore(string text)
    {
        try
        {
            var package = new DataPackage { RequestedOperation = DataPackageOperation.Copy };
            package.SetText(text);
            Clipboard.SetContent(package);
            Clipboard.Flush(); // keeps the text after the application exits
        }
        catch (Exception ex) when (ex is COMException or InvalidOperationException)
        {
            Debug.WriteLine($"LoomTerminal: clipboard write failed: {ex.Message}");
        }

        return Task.FromResult(true);
    }
}
