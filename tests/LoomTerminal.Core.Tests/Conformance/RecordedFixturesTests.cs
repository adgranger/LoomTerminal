namespace LoomTerminal.Core.Tests.Conformance;

public sealed class RecordedFixturesTests
{
    private static string RecordedFolder => Path.Combine(AppContext.BaseDirectory, "Conformance", "Fixtures", "recorded");

    public static IEnumerable<object[]> RecordingNames() =>
        Directory.GetFiles(RecordedFolder, "*.vtrec")
            .Select(path => Path.GetFileNameWithoutExtension(path))
            .Order(StringComparer.Ordinal)
            .Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(RecordingNames))]
    public void Recording_is_readable_and_replays(string name)
    {
        FixtureScript script = FixtureReader.ReadRecording(File.ReadAllText(Path.Combine(RecordedFolder, name + ".vtrec")));

        Assert.NotEmpty(script.Events);
        ScreenSnapshot snapshot = FixtureReader.Replay(script);
        Assert.True(snapshot.Cols > 0);
    }
}
