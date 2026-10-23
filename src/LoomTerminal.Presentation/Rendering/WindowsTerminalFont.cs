using System.Text.Json;

namespace LoomTerminal.Presentation.Rendering;

/// <summary>The font of the user's Windows Terminal default profile, read on request.</summary>
/// <remarks>
/// Threading: stateless; any thread. The settings of another application are read only when a host calls
/// <see cref="Read"/>; nothing read leaves the process.
/// </remarks>
public static class WindowsTerminalFont
{
    private const string DefaultFace = "Cascadia Mono";
    private const double DefaultPoints = 12;
    private const int DefaultWeight = 400;

    /// <summary>The Windows Terminal defaults: Cascadia Mono, 12 pt (16 DIPs), regular weight.</summary>
    public static TerminalFont Default { get; } = new([DefaultFace], PointsToDips(DefaultPoints));

    /// <summary>Reads the font of the default profile from the first Windows Terminal settings file found.</summary>
    /// <returns>That font, or <see cref="Default"/> when no settings file is readable.</returns>
    public static TerminalFont Read()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        foreach (var path in SettingsPaths(localAppData))
        {
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                return Parse(File.ReadAllText(path));
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                return Default;
            }
        }

        return Default;
    }

    internal static IReadOnlyList<string> SettingsPaths(string localAppData) =>
    [
        Path.Combine(localAppData, "Packages", "Microsoft.WindowsTerminal_8wekyb3d8bbwe", "LocalState", "settings.json"),
        Path.Combine(localAppData, "Packages", "Microsoft.WindowsTerminalPreview_8wekyb3d8bbwe", "LocalState", "settings.json"),
        Path.Combine(localAppData, "Microsoft", "Windows Terminal", "settings.json"),
    ];

    internal static TerminalFont Parse(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(
                json,
                new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
            return FromRoot(document.RootElement);
        }
        catch (JsonException)
        {
            return Default;
        }
    }

    private static TerminalFont FromRoot(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return Default;
        }

        JsonElement? defaults = null;
        JsonElement? list = null;
        if (root.TryGetProperty("profiles", out var profiles))
        {
            if (profiles.ValueKind == JsonValueKind.Object)
            {
                defaults = profiles.TryGetProperty("defaults", out var d) ? d : null;
                list = profiles.TryGetProperty("list", out var l) ? l : null;
            }
            else if (profiles.ValueKind == JsonValueKind.Array)
            {
                list = profiles;
            }
        }

        JsonElement? profile = null;
        if (list is { ValueKind: JsonValueKind.Array } entries
            && root.TryGetProperty("defaultProfile", out var id)
            && id.ValueKind == JsonValueKind.String)
        {
            profile = Find(entries, "guid", id.GetString()) ?? Find(entries, "name", id.GetString());
        }

        JsonElement? Pick(string name, string legacy)
        {
            foreach (var source in new[] { profile, defaults })
            {
                if (source is not { ValueKind: JsonValueKind.Object } s)
                {
                    continue;
                }

                if (s.TryGetProperty("font", out var font) && font.ValueKind == JsonValueKind.Object
                    && font.TryGetProperty(name, out var value))
                {
                    return value;
                }

                if (s.TryGetProperty(legacy, out var old))
                {
                    return old;
                }
            }

            return null;
        }

        return new TerminalFont(
            Faces(Pick("face", "fontFace")),
            PointsToDips(Points(Pick("size", "fontSize"))),
            Weight: Weight(Pick("weight", "fontWeight")));
    }

    private static JsonElement? Find(JsonElement entries, string property, string? value)
    {
        foreach (var entry in entries.EnumerateArray())
        {
            if (entry.ValueKind == JsonValueKind.Object
                && entry.TryGetProperty(property, out var p)
                && p.ValueKind == JsonValueKind.String
                && string.Equals(p.GetString(), value, StringComparison.OrdinalIgnoreCase))
            {
                return entry;
            }
        }

        return null;
    }

    private static string[] Faces(JsonElement? value)
    {
        if (value is { ValueKind: JsonValueKind.String } v)
        {
            var faces = v.GetString()!.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (faces.Length > 0)
            {
                return faces;
            }
        }

        return [DefaultFace];
    }

    private static double Points(JsonElement? value) =>
        value is { ValueKind: JsonValueKind.Number } v && v.TryGetDouble(out var points) && points > 0
            ? points
            : DefaultPoints;

    private static int Weight(JsonElement? value)
    {
        if (value is { ValueKind: JsonValueKind.Number } n)
        {
            return n.TryGetInt32(out var weight) && weight is >= 1 and <= 1000 ? weight : DefaultWeight;
        }

        return value is { ValueKind: JsonValueKind.String } s
            ? s.GetString() switch
            {
                "thin" => 100,
                "extra-light" => 200,
                "light" => 300,
                "semi-light" => 350,
                "normal" => 400,
                "medium" => 500,
                "semi-bold" => 600,
                "bold" => 700,
                "extra-bold" => 800,
                "black" => 900,
                "extra-black" => 950,
                _ => DefaultWeight,
            }
            : DefaultWeight;
    }

    private static double PointsToDips(double points) => points * 96 / 72;
}
