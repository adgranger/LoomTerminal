namespace LoomTerminal.Presentation.Links;

/// <summary>The default handler: accepts <c>https</c> links only and opens nothing, the host sets its own.</summary>
/// <remarks>Threading: stateless; any thread.</remarks>
internal sealed class HttpsOnlyLinkHandler : ILinkHandler
{
    /// <inheritdoc />
    public bool CanOpen(Uri uri) => uri.Scheme == Uri.UriSchemeHttps;

    /// <inheritdoc />
    public void Open(Uri uri)
    {
    }
}
