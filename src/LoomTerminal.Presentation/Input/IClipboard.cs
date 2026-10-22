namespace LoomTerminal.Presentation.Input;

/// <summary>The system clipboard, implemented by the UI backend.</summary>
/// <remarks>Threading: <see cref="Session.TerminalSession"/> calls it on the thread that triggered the copy or paste (the UI thread).</remarks>
public interface IClipboard
{
    /// <summary>Gets the text of the clipboard.</summary>
    /// <returns>The text, or <see langword="null"/> when the clipboard holds none.</returns>
    Task<string?> GetTextAsync();

    /// <summary>Replaces the content of the clipboard with <paramref name="text"/>.</summary>
    /// <param name="text">The text to copy.</param>
    /// <returns>A task that completes when the clipboard is set.</returns>
    Task SetTextAsync(string text);
}
