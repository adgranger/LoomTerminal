namespace LoomTerminal.Presentation.Links;

/// <summary>Decides which links the terminal offers and opens the ones the user activates with Ctrl+click.</summary>
/// <remarks>
/// Threading: <see cref="Session.TerminalSession"/> calls both methods on its parser thread; <see cref="Open"/> must
/// hand over to the UI thread if it needs it.
/// </remarks>
public interface ILinkHandler
{
    /// <summary>Whether a link may be shown and opened; links that are refused are never underlined.</summary>
    /// <param name="uri">The target of the link.</param>
    /// <returns><see langword="true"/> to offer the link.</returns>
    bool CanOpen(Uri uri);

    /// <summary>Opens a link the user activated; only called for a link accepted by <see cref="CanOpen"/>.</summary>
    /// <param name="uri">The target of the link.</param>
    void Open(Uri uri);
}
