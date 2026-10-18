// Ported from xtermjs/xterm.js: src/common/services/OscLinkService.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Buffer;

namespace LoomTerminal.Core.Services;

/// <summary>The registry of OSC 8 hyperlinks, backed by markers that free a link when its lines are trimmed.</summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal sealed class OscLinkService(IBufferService bufferService) : IOscLinkService
{
    // A map of the link key to link entry. This is used to add additional lines to links with ids.
    private readonly Dictionary<string, Entry> _entriesWithId = [];

    // A map of the link id to the link entry. The "link id" (number) which is the numeric representation of a unique
    // link should not be confused with "id" (string) which comes in with `id=` in the OSC link's properties.
    private readonly Dictionary<int, Entry> _dataByLinkId = [];

    private int _nextId = 1;

    /// <inheritdoc />
    public int RegisterLink(OscLinkData data)
    {
        Core.Buffer.Buffer buffer = bufferService.Buffer;

        // Links with no id will only ever be registered a single time
        if (data.Id is null)
        {
            Marker noIdMarker = buffer.AddMarker(buffer.YBase + buffer.Y);
            Entry noIdEntry = new(_nextId++, data, key: null, noIdMarker);
            noIdMarker.OnDispose.Subscribe(_ => RemoveMarkerFromLink(noIdEntry, noIdMarker));
            _dataByLinkId[noIdEntry.Id] = noIdEntry;
            return noIdEntry.Id;
        }

        // Add the line to the link if it already exists
        string key = GetEntryIdKey(data);
        if (_entriesWithId.TryGetValue(key, out Entry? match))
        {
            AddLineToLink(match.Id, buffer.YBase + buffer.Y);
            return match.Id;
        }

        // Create the link
        Marker marker = buffer.AddMarker(buffer.YBase + buffer.Y);
        Entry entry = new(_nextId++, data, key, marker);
        marker.OnDispose.Subscribe(_ => RemoveMarkerFromLink(entry, marker));
        _entriesWithId[key] = entry;
        _dataByLinkId[entry.Id] = entry;
        return entry.Id;
    }

    /// <inheritdoc />
    public void AddLineToLink(int linkId, int y)
    {
        if (!_dataByLinkId.TryGetValue(linkId, out Entry? entry))
        {
            return;
        }

        if (entry.Lines.TrueForAll(e => e.Line != y))
        {
            Marker marker = bufferService.Buffer.AddMarker(y);
            entry.Lines.Add(marker);
            marker.OnDispose.Subscribe(_ => RemoveMarkerFromLink(entry, marker));
        }
    }

    /// <inheritdoc />
    public OscLinkData? GetLinkData(int linkId) =>
        _dataByLinkId.TryGetValue(linkId, out Entry? entry) ? entry.Data : null;

    private static string GetEntryIdKey(OscLinkData linkData) => $"{linkData.Id};;{linkData.Uri}";

    private void RemoveMarkerFromLink(Entry entry, Marker marker)
    {
        int index = entry.Lines.IndexOf(marker);
        if (index == -1)
        {
            return;
        }

        entry.Lines.RemoveAt(index);
        if (entry.Lines.Count == 0)
        {
            if (entry.Data.Id is not null)
            {
                _entriesWithId.Remove(entry.Key!);
            }

            _dataByLinkId.Remove(entry.Id);
        }
    }

    private sealed class Entry(int id, OscLinkData data, string? key, Marker firstMarker)
    {
        public int Id { get; } = id;

        public OscLinkData Data { get; } = data;

        public string? Key { get; } = key;

        public List<Marker> Lines { get; } = [firstMarker];
    }
}
