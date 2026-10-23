# P4-27 — Windows Terminal font: defaults, and the user's settings on request

- **Status:** todo
- **Depends on:** P4-26
- **Size:** M

## Goal

`WindowsTerminalFont.Read()` returns the font of the user's Windows Terminal default profile (face list, size,
weight), or Windows Terminal's defaults when there is no readable settings file. The control never calls it by itself.
The Gallery shows font sizes in points like Windows Terminal (12 pt by default) and has a switch, **off by default**,
"Font from Windows Terminal".

## Verified facts

- Windows Terminal docs (learn.microsoft.com, "Appearance profile settings", read 2026-10-23): the `font` object has
  `face` (default `"Cascadia Mono"`), `size` "in points" (default `12`), `weight` (`"normal"`, `"thin"`,
  `"extra-light"`, `"light"`, `"semi-light"`, `"medium"`, `"semi-bold"`, `"bold"`, `"extra-bold"`, `"black"`,
  `"extra-black"`, or an integer OpenType weight; default `"normal"`). Before Windows Terminal 1.10 the same settings
  were `fontFace`, `fontSize`, `fontWeight` on the profile. Settings in `profiles.defaults` apply to every profile;
  profiles are in `profiles.list`.
- Settings file of the Store build: `%LOCALAPPDATA%\Packages\Microsoft.WindowsTerminal_8wekyb3d8bbwe\LocalState\settings.json`
  (exists on the author's machine). Preview: same with `Microsoft.WindowsTerminalPreview_8wekyb3d8bbwe`. Unpackaged
  builds: `%LOCALAPPDATA%\Microsoft\Windows Terminal\settings.json` (from memory, not verified).
- The file is JSON with comments and trailing commas.

## Files to create or change

- `src/LoomTerminal.Presentation/Rendering/WindowsTerminalFont.cs` — create (`public static class`)
- `src/LoomTerminal.Presentation/PublicAPI.Unshipped.txt` — via `eng/Update-PublicApi.ps1`
- `tests/LoomTerminal.Presentation.Tests/Rendering/WindowsTerminalFontTests.cs` — create
- `samples/LoomTerminal.Gallery/MainWindow.xaml(.cs)` — size in points, the switch
- `src/LoomTerminal.WinUI/README.md` (section "Fonts"), `docs/manual-tests.md` — update

## Design decisions (already made — do not change)

- **Privacy/behavior (user to confirm):** the library reads another application's settings **only when the host calls
  `Read()`**; the control and the default Gallery start never do. The Gallery switch is off by default. Nothing read
  leaves the process. Reason: safest default; a host opts in explicitly.
- Public surface (XML docs, threading note "stateless; any thread"):
  `public static TerminalFont Default { get; }` = `new(["Cascadia Mono"], 16)` (12 pt × 96 / 72 = 16 DIPs, weight 400);
  `public static TerminalFont Read()`. Internal: `static TerminalFont Parse(string json)`,
  `static IReadOnlyList<string> SettingsPaths(string localAppData)` (the three paths above, in that order).
- `Read()`: first existing path → `Parse(File.ReadAllText(path))`; `IOException` / `UnauthorizedAccessException` →
  `Default`. No path → `Default`.
- `Parse(json)`: `JsonDocument.Parse(json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true })`
  (no serializer, no reflection: Native AOT-safe); `JsonException` → `Default`.
  - `profiles` is an object → `defaults` = `profiles.defaults`, `list` = `profiles.list`; `profiles` is an array →
    `list` = it, no defaults.
  - Default profile = the `list` entry whose `guid` equals root `defaultProfile` (`OrdinalIgnoreCase`), else whose
    `name` equals it, else none.
  - For each of face / size / weight, the first value found in: profile `font.<x>`, profile legacy
    (`fontFace`/`fontSize`/`fontWeight`), defaults `font.<x>`, defaults legacy, built-in default.
  - face: split on `,`, trim, drop empties (no face left → default). size: JSON number > 0 (points, may be decimal) →
    DIPs = `points * 96 / 72`; anything else → default. weight: JSON integer 1–1000, or one of the names above mapped to
    100, 200, 300, 350, 400, 500, 600, 700, 800, 900, 950; anything else → 400.
  - `LineHeight`, `LetterSpacing`, `BoldWeight` keep the `TerminalFont` defaults (Windows Terminal's `cellHeight` and
    `features` are ignored).
- Gallery:
  - Font size slider: header "Font size (pt)", `Minimum` 6, `Maximum` 24, `Value` 12, `StepFrequency` 1;
    `terminal.FontSize = points * 96 / 72`. Reason: same numbers as Windows Terminal; 12 pt is its default size.
  - New `ToggleSwitch x:Name="WindowsTerminalFontSwitch" Header="Font from Windows Terminal"` (off), placed after the
    font family box. When on: `WindowsTerminalFont.Read()` once (kept in a field until switched off), and
    `ApplySettings` sets `FontFamily = new FontFamily(string.Join(", ", font.Families))`, `FontSize = font.Size`,
    `FontWeight = new FontWeight { Weight = (ushort)font.Weight }`; the family box and the size slider are disabled.
    When off: today's behavior, `FontWeight = FontWeights.Normal`, both controls enabled.
  - The control's style defaults (`FontFamily`, `FontSize` 12 DIPs) are **not** changed. Reason: the control speaks
    XAML DIPs; VIGO sets its own font.

## Tests

- [ ] New: `Empty_settings_give_the_Windows_Terminal_defaults` (`"{}"` → Cascadia Mono, 16, 400)
- [ ] New: `Defaults_font_applies_to_the_default_profile`
- [ ] New: `The_default_profile_overrides_the_defaults` (by guid, mixed case)
- [ ] New: `The_default_profile_can_be_named`
- [ ] New: `Legacy_font_properties_are_read`
- [ ] New: `Comments_and_trailing_commas_are_accepted`
- [ ] New: `A_face_list_is_split_on_commas`
- [ ] New: `Size_is_converted_from_points_to_DIPs` (10.5 pt → 14)
- [ ] New: `Theory: weight names and numbers` (`"semi-light"` 350, `"bold"` 700, `"extra-black"` 950, `600` 600,
  `"heavy"` 400, `0` 400)
- [ ] New: `Profiles_as_an_array_are_read`
- [ ] New: `Malformed_json_gives_the_defaults`
- [ ] New: `Settings_paths_are_the_Store_Preview_then_unpackaged_files`
- [ ] Manual (AOT gallery): the switch on applies the face/size/weight of Windows Terminal's default profile (compare
  side by side with Windows Terminal: same glyph size and weight); off restores the gallery settings.

## Acceptance criteria

- [ ] `rtk dotnet build LoomTerminal.slnx -c Release` — 0 warnings
- [ ] `rtk test dotnet test --solution LoomTerminal.slnx -c Release` — all green
- [ ] `rtk proxy pwsh -NoProfile -File eng/Update-PublicApi.ps1 src/LoomTerminal.Presentation` — run, public API file updated
- [ ] `rtk proxy pwsh -NoProfile -File eng/Test-AotApp.ps1 samples/LoomTerminal.Gallery` — exit code 0
- [ ] **Manual (human):** the side-by-side check above, ticked in `docs/manual-tests.md`
- [ ] Card status and `docs/tasks/README.md` updated

## Out of scope

- Per-profile fonts matched to the shell of a tab, color schemes, `cellHeight`, font features and axes, watching the
  settings file for changes.
- Calling `Read()` from the control or by default anywhere.

## Suggested commit message

`feat(presentation): read the Windows Terminal font on request; gallery sizes in points`
