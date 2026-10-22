// Inspired by VIGO: src/Vigo.App/Controls/TerminalView.cs (Copy, PasteAsync)
using System.Diagnostics;
using System.Runtime.InteropServices;
using LoomTerminal.Presentation.Input;
using Windows.ApplicationModel.DataTransfer;

namespace LoomTerminal.WinUI.Input;

/// <summary>The WinRT clipboard behind <see cref="IClipboard"/>.</summary>
/// <remarks>Threading: UI thread only (the WinRT clipboard requires it).</remarks>
internal sealed class WinRtClipboard : IClipboard
{
    /// <inheritdoc />
    public async Task<string?> GetTextAsync()
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
    public Task SetTextAsync(string text)
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

        return Task.CompletedTask;
    }
}
