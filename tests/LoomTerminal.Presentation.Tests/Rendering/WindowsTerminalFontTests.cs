using LoomTerminal.Presentation.Rendering;

namespace LoomTerminal.Presentation.Tests.Rendering;

public class WindowsTerminalFontTests
{
    [Fact]
    public void Empty_settings_give_the_Windows_Terminal_defaults()
    {
        var font = WindowsTerminalFont.Parse("{}");

        Assert.Equal(["Cascadia Mono"], font.Families);
        Assert.Equal(16, font.Size);
        Assert.Equal(400, font.Weight);
    }

    [Fact]
    public void Defaults_font_applies_to_the_default_profile()
    {
        var font = WindowsTerminalFont.Parse("""
            { "defaultProfile": "{a}",
              "profiles": { "defaults": { "font": { "face": "Fira Code", "size": 9, "weight": "bold" } },
                            "list": [ { "guid": "{a}", "name": "PS" } ] } }
            """);

        Assert.Equal(["Fira Code"], font.Families);
        Assert.Equal(12, font.Size);
        Assert.Equal(700, font.Weight);
    }

    [Fact]
    public void The_default_profile_overrides_the_defaults()
    {
        var font = WindowsTerminalFont.Parse("""
            { "defaultProfile": "{ABC}",
              "profiles": { "defaults": { "font": { "face": "Fira Code", "size": 9 } },
                            "list": [ { "guid": "{abc}", "font": { "face": "Consolas" } } ] } }
            """);

        Assert.Equal(["Consolas"], font.Families);
        Assert.Equal(12, font.Size);
    }

    [Fact]
    public void The_default_profile_can_be_named()
    {
        var font = WindowsTerminalFont.Parse("""
            { "defaultProfile": "Ubuntu",
              "profiles": { "list": [ { "guid": "{x}", "name": "Other", "font": { "face": "A" } },
                                      { "guid": "{y}", "name": "Ubuntu", "font": { "face": "B" } } ] } }
            """);

        Assert.Equal(["B"], font.Families);
    }

    [Fact]
    public void Legacy_font_properties_are_read()
    {
        var font = WindowsTerminalFont.Parse("""
            { "defaultProfile": "{a}",
              "profiles": { "defaults": { "fontWeight": "light" },
                            "list": [ { "guid": "{a}", "fontFace": "Consolas", "fontSize": 18 } ] } }
            """);

        Assert.Equal(["Consolas"], font.Families);
        Assert.Equal(24, font.Size);
        Assert.Equal(300, font.Weight);
    }

    [Fact]
    public void Comments_and_trailing_commas_are_accepted()
    {
        var font = WindowsTerminalFont.Parse("""
            { // comment
              "profiles": { "defaults": { "font": { "face": "Fira Code", /* c */ }, }, },
            }
            """);

        Assert.Equal(["Fira Code"], font.Families);
    }

    [Fact]
    public void A_face_list_is_split_on_commas()
    {
        var font = WindowsTerminalFont.Parse("""{ "profiles": { "defaults": { "font": { "face": " A ,B,, C" } } } }""");

        Assert.Equal(["A", "B", "C"], font.Families);
    }

    [Fact]
    public void Size_is_converted_from_points_to_DIPs()
    {
        var font = WindowsTerminalFont.Parse("""{ "profiles": { "defaults": { "font": { "size": 10.5 } } } }""");

        Assert.Equal(14, font.Size);
    }

    [Theory]
    [InlineData("\"semi-light\"", 350)]
    [InlineData("\"bold\"", 700)]
    [InlineData("\"extra-black\"", 950)]
    [InlineData("600", 600)]
    [InlineData("\"heavy\"", 400)]
    [InlineData("0", 400)]
    public void Weight_names_and_numbers(string json, int expected)
    {
        var font = WindowsTerminalFont.Parse("{ \"profiles\": { \"defaults\": { \"font\": { \"weight\": " + json + " } } } }");

        Assert.Equal(expected, font.Weight);
    }

    [Fact]
    public void Profiles_as_an_array_are_read()
    {
        var font = WindowsTerminalFont.Parse("""
            { "defaultProfile": "{a}", "profiles": [ { "guid": "{a}", "font": { "face": "Consolas" } } ] }
            """);

        Assert.Equal(["Consolas"], font.Families);
    }

    [Fact]
    public void Malformed_json_gives_the_defaults()
    {
        var font = WindowsTerminalFont.Parse("{ not json");

        Assert.Equal(WindowsTerminalFont.Default.Families, font.Families);
        Assert.Equal(WindowsTerminalFont.Default.Size, font.Size);
        Assert.Equal(WindowsTerminalFont.Default.Weight, font.Weight);
    }

    [Fact]
    public void Settings_paths_are_the_Store_Preview_then_unpackaged_files()
    {
        var paths = WindowsTerminalFont.SettingsPaths(@"C:\L");

        Assert.Equal(3, paths.Count);
        Assert.Contains("Microsoft.WindowsTerminal_8wekyb3d8bbwe", paths[0]);
        Assert.Contains("Microsoft.WindowsTerminalPreview_8wekyb3d8bbwe", paths[1]);
        Assert.EndsWith(Path.Combine("Microsoft", "Windows Terminal", "settings.json"), paths[2]);
    }
}
