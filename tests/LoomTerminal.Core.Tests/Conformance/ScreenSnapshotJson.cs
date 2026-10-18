using System.Text.Json;
using System.Text.Json.Serialization;

namespace LoomTerminal.Core.Tests.Conformance;

/// <summary>Source-generated JSON metadata of <see cref="ScreenSnapshot"/> (no reflection, Native AOT friendly).</summary>
/// <remarks>Threading: stateless.</remarks>
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ScreenSnapshot))]
internal sealed partial class ScreenSnapshotJsonContext : JsonSerializerContext
{
    /// <summary>Normalized JSON text of a snapshot: 2-space indentation, <c>\n</c> line endings, no trailing newline.</summary>
    internal static string ToJson(ScreenSnapshot snapshot) =>
        JsonSerializer.Serialize(snapshot, Default.ScreenSnapshot).ReplaceLineEndings("\n");

    /// <summary>Parses a snapshot, then normalizes it again, so that expected files compare with <see cref="ToJson"/>.</summary>
    internal static string Normalize(string json) =>
        ToJson(JsonSerializer.Deserialize(json, Default.ScreenSnapshot)!);
}
