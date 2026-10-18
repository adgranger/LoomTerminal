// Ported from xtermjs/xterm.js: src/common/services/OscLinkService.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Buffer;
using LoomTerminal.Core.Services;

namespace LoomTerminal.Core.Tests.Services;

public sealed class OscLinkServiceTests : IDisposable
{
    private readonly OptionsService _optionsService = new(TerminalOptions.Default with { Rows = 3, Cols = 10 });
    private readonly BufferService _bufferService;
    private readonly OscLinkService _oscLinkService;

    public OscLinkServiceTests()
    {
        _bufferService = new BufferService(_optionsService);
        _oscLinkService = new OscLinkService(_bufferService);
    }

    public void Dispose()
    {
        _bufferService.Dispose();
        _optionsService.Dispose();
    }

    // xterm.js: 'OscLinkService' › 'constructor' › 'link IDs are created and fetched consistently'
    [Fact]
    public void Link_IDs_are_created_and_fetched_consistently()
    {
        int linkId = _oscLinkService.RegisterLink(new OscLinkData("foo", "bar"));
        Assert.NotEqual(0, linkId);
        Assert.Equal(linkId, _oscLinkService.RegisterLink(new OscLinkData("foo", "bar")));
    }

    // xterm.js: 'OscLinkService' › 'constructor' › 'should dispose the link ID when the last marker is trimmed from the buffer'
    [Fact]
    public void The_link_ID_is_disposed_when_the_last_marker_is_trimmed_from_the_buffer()
    {
        // Activate the alt buffer to get 0 scrollback
        _bufferService.Buffers.ActivateAltBuffer();
        int linkId = _oscLinkService.RegisterLink(new OscLinkData("foo", "bar"));
        Assert.NotEqual(0, linkId);
        _bufferService.Scroll(new AttributeData());
        Assert.NotEqual(linkId, _oscLinkService.RegisterLink(new OscLinkData("foo", "bar")));
    }

    // xterm.js: 'OscLinkService' › 'constructor' › 'should fetch link data from link id'
    [Fact]
    public void Link_data_is_fetched_from_the_link_id()
    {
        int linkId = _oscLinkService.RegisterLink(new OscLinkData("foo", "bar"));
        Assert.Equal(new OscLinkData("foo", "bar"), _oscLinkService.GetLinkData(linkId));
    }

    [Fact]
    public void Links_without_an_id_are_registered_every_time_and_unknown_ids_have_no_data()
    {
        int first = _oscLinkService.RegisterLink(new OscLinkData(null, "bar"));
        int second = _oscLinkService.RegisterLink(new OscLinkData(null, "bar"));
        Assert.NotEqual(first, second);
        Assert.Null(_oscLinkService.GetLinkData(999));
    }

    [Fact]
    public void AddLineToLink_keeps_the_link_alive_until_its_last_line_is_gone()
    {
        int linkId = _oscLinkService.RegisterLink(new OscLinkData("foo", "bar"));
        _oscLinkService.AddLineToLink(linkId, 1);
        _oscLinkService.AddLineToLink(linkId, 1);
        _oscLinkService.AddLineToLink(999, 1);
        Assert.Equal(2, _bufferService.Buffer.Markers.Count);
        _bufferService.Buffer.ClearMarkers(1);
        Assert.NotNull(_oscLinkService.GetLinkData(linkId));
        _bufferService.Buffer.ClearAllMarkers();
        Assert.Null(_oscLinkService.GetLinkData(linkId));
    }
}
