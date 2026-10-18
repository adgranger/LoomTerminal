namespace LoomTerminal.Core.Tests.Conformance;

public sealed class ScreenSnapshotTests
{
    private static ScreenSnapshot ReplayInput(string input) => FixtureReader.Replay(FixtureReader.ReadInput(input));

    [Fact]
    public void Colored_text_is_captured_as_runs_with_colors_and_flags()
    {
        ScreenSnapshot snapshot = ReplayInput("hello\\r\\n\\e[1;31mred\\e[0m");

        Assert.Equal(80, snapshot.Cols);
        Assert.Equal(24, snapshot.Rows);
        Assert.Equal(24, snapshot.Lines.Count);
        Assert.Equal("hello", snapshot.Lines[0].Text);
        Assert.Equal("red", snapshot.Lines[1].Text);
        RunSnapshot run = Assert.Single(snapshot.Lines[1].Runs);
        Assert.Equal(0, run.Start);
        Assert.Equal(3, run.Length);
        Assert.Equal("p16:1", run.Fg);
        Assert.Equal("default", run.Bg);
        Assert.Equal(["bold"], run.Flags);
        Assert.Equal(new CursorSnapshot(3, 1, true), snapshot.Cursor);
    }

    [Fact]
    public void A_wide_character_produces_one_run_covering_two_cells()
    {
        ScreenSnapshot snapshot = ReplayInput("\\u4e2d");

        RunSnapshot run = Assert.Single(snapshot.Lines[0].Runs);
        Assert.Equal(0, run.Start);
        Assert.Equal(2, run.Length);
    }

    [Fact]
    public void Resize_directives_and_the_size_directive_are_applied()
    {
        ScreenSnapshot sized = ReplayInput("#!size 20 5\nabc");
        Assert.Equal(20, sized.Cols);
        Assert.Equal(5, sized.Rows);

        ScreenSnapshot resized = ReplayInput("abc\n#!resize 40 10\n");
        Assert.Equal(40, resized.Cols);
        Assert.Equal(10, resized.Rows);
        Assert.Equal(10, resized.Lines.Count);
    }

    [Fact]
    public void A_recording_event_writes_the_decoded_bytes()
    {
        ScreenSnapshot snapshot = FixtureReader.Replay(FixtureReader.ReadRecording("O aGVsbG8=\nR 40 10\n"));

        Assert.Equal("hello", snapshot.Lines[0].Text);
        Assert.Equal(40, snapshot.Cols);
    }

    [Fact]
    public void An_unknown_escape_names_its_line()
    {
        FormatException error = Assert.Throws<FormatException>(() => FixtureReader.ReadInput("ok\nbad \\q"));
        Assert.Contains("Line 2", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Modes_title_and_the_active_buffer_are_captured()
    {
        ScreenSnapshot snapshot = ReplayInput("\\e]2;my title\\a\\e[?1h\\e[?2004h\\e[?25l\\e[4h\\e[?1049h");

        Assert.Equal("my title", snapshot.Title);
        Assert.Equal("alternate", snapshot.ActiveBuffer);
        Assert.False(snapshot.Cursor.Visible);
        Assert.Equal(new ModesSnapshot(true, true, true, false, true, "none"), snapshot.Modes);
    }

    [Fact]
    public void Underline_styles_and_rgb_colors_use_the_convention_names()
    {
        ScreenSnapshot snapshot = ReplayInput("\\e[4:3;38;2;1;2;255;48;5;200mx\\e[0m\\e[9;53my");

        Assert.Equal(["underline:curly"], snapshot.Lines[0].Runs[0].Flags);
        Assert.Equal("rgb:0102ff", snapshot.Lines[0].Runs[0].Fg);
        Assert.Equal("p256:200", snapshot.Lines[0].Runs[0].Bg);
        Assert.Equal(["strikethrough", "overline"], snapshot.Lines[0].Runs[1].Flags);
    }

    [Fact]
    public void The_json_has_the_documented_shape_and_round_trips()
    {
        ScreenSnapshot snapshot = ReplayInput("#!size 5 1\nhi");
        string json = ScreenSnapshotJsonContext.ToJson(snapshot);

        Assert.StartsWith("{\n  \"cols\": 5,\n  \"rows\": 1,\n  \"cursor\": {\n    \"x\": 2,", json, StringComparison.Ordinal);
        Assert.Equal(json, ScreenSnapshotJsonContext.Normalize(json));
    }
}
