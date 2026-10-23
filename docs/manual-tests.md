# Manual tests

The gallery is the manual test bench of the WinUI control. Run every item once in the **AOT-published** gallery
(a Debug build is fine for a quick loop) and tick it:

```powershell
rtk dotnet publish samples/LoomTerminal.Gallery -c Release -r win-x64
# then run samples/LoomTerminal.Gallery/bin/Release/net11.0-windows10.0.26100.0/win-x64/publish/LoomTerminal.Gallery.exe
```

Gallery basics: one tab per shell (the **+** button lists `Shells.Detect()`), Ctrl+Shift+T opens a tab with the same
shell (a `KeyPreview` handler), the right pane holds the settings (theme, font, size, line height, cursor style, blink),
the status bar shows `columns×rows` and the draw time of the last frame. Closing a tab kills its shell.

## Demo scripts

Next to the exe, in `Demos\` (run them in pwsh 7: `& .\Demos\colors.ps1`):

| Script | Shows |
|---|---|
| `colors.ps1` | 16, 256 and truecolor tables |
| `styles.ps1` | SGR styles, underline variants and colors, overline, cursor shapes (DECSCUSR 1–6) |
| `unicode.ps1` | CJK, emoji, combining marks, box drawing, Powerline shapes |
| `stress.ps1` | about 10 MB of colored output with the elapsed time |

## VIGO parity (`docs/ROADMAP.md`)

- [ ] Scrollback 10,000 lines; cursor blink; font size 12, line height 1.15 (gallery defaults)
- [ ] Font stack Cascadia Mono / Cascadia Code, then Nerd Font fallbacks (oh-my-posh, starship glyphs)
- [ ] Unicode 11 character widths (`unicode.ps1`: the bars of the "Grid" line match the ruler)
- [ ] ConPTY-aware reflow: shrink and widen the window with long lines on screen, no duplicated or lost text
- [ ] Theme set live by the host: the theme switch changes background, foreground, cursor and selection of every tab
- [ ] Links: Ctrl+click on an https URL raises `LinkActivated`; a plain click only selects
- [ ] Keyboard: AltGr types characters, never shortcuts; Ctrl+Shift+T is claimed by the host (new tab)
- [ ] Copy: Ctrl+Shift+C, or Ctrl+C when there is a selection (otherwise Ctrl+C interrupts)
- [ ] Paste: Ctrl+V, Ctrl+Shift+V, Shift+Insert, bracketed
- [ ] Right click: copies the selection, or pastes when there is none
- [ ] Resize to the control size (switching tabs and minimizing do not break the grid)
- [ ] Shell exit reported to the host: `exit` shows `[exited <code>]` in the tab header

## Rendering (P4-03, P4-04, P4-05, P4-06)

- [ ] `dir`, a colored `git log --graph` and `echo 日本語 😀` stay on the grid; resizing keeps text crisp at 100 % and 150 %
- [ ] An oh-my-posh or starship prompt renders with seamless separators
- [ ] `styles.ps1`: five underline styles, strikethrough, overline; DECSCUSR 1–6 change the cursor shape; an unfocused window shows the outline cursor
- [ ] `mode con` (cmd) reports the size shown in the status bar after a resize
- [ ] No redraw loop when idle (Task Manager CPU ≈ 0 %)
- [ ] Moving the window between 100 % and 150 % monitors keeps text crisp

## Input (P4-07, P4-08, P4-09, P4-10, P4-11)

- [ ] Arrows, Home/End, F1–F12 with modifiers work in `vim` / `htop` (WSL)
- [ ] AltGr+é/@/# types characters on an AZERTY layout
- [ ] Ctrl+C interrupts `ping -t`; Tab completes in pwsh (focus stays in the terminal)
- [ ] `ê` via dead key `^` then `e`; an emoji from the Windows emoji panel (Win+.) arrives as one character
- [ ] Alt+b moves a word back in bash (sends `ESC b`) without typing `b`
- [ ] IME (Windows 11, Microsoft IME Japanese): `nihongo`, convert, commit gives `日本語`; the candidate window is next to the cursor; Escape cancels without sending
- [ ] Select by drag, double click (word), triple click (line); Alt+drag selects a column
- [ ] The wheel scrolls the scrollback in pwsh, and scrolls inside `less` / `vim` (alt screen, arrow keys)
- [ ] `htop` receives clicks (mouse reporting)
- [ ] Ctrl+hover underlines an https link; Ctrl+click raises `LinkActivated`
- [ ] Ctrl+Shift+C / Ctrl+V / Shift+Insert / right click behave as VIGO; a multi-line paste into pwsh is bracketed (PSReadLine does not run each line)

## Scrollbar, focus, settings (P4-12, P4-13, P4-14)

- [ ] `dir /s C:\Windows`, then scroll with the bar, the wheel and Shift+PageUp; new output while scrolled up does not jump
- [ ] Typing jumps to the bottom; the bar disappears in `vim`
- [ ] The cursor blinks only while focused (blink switch on); an inactive window shows the outline cursor
- [ ] `echo -e "\a"` (WSL) raises `BellRang`
- [ ] Theme switch, font family, font size, line height, cursor style and blink all apply live to every tab without restarting the shell

## Accessibility (P4-15)

- [ ] Narrator reads output as it arrives
- [ ] Narrator's line navigation reads the visible lines
- [ ] Accessibility Insights "FastPass" reports no error on the gallery

## Gallery itself (P4-16)

- [ ] The **+** menu lists the installed shells; each one starts in a new tab
- [ ] Closing a tab kills its shell (no leftover `pwsh` / `conhost` / `OpenConsole` in Task Manager); closing the window kills all
- [ ] The status bar follows the selected tab and a window resize
- [ ] `stress.ps1` completes; the window stays responsive and the frame time stays reasonable

## Gallery tabs (P4-24)

- [ ] Every shell of the **+** menu opens a tab with its icon and its friendly name; `pwsh` shows "PowerShell 7", not the WindowsApps path (if the Store PowerShell has no icon, write it in the card's Notes: follow-up card)
- [ ] `$Host.UI.RawUI.WindowTitle = 'hello'` in pwsh changes the header to "hello"; the tooltip shows "PowerShell 7 — hello"

## Glyph font fallback (P4-26)

- [ ] Font family **Cascadia Mono**, a Nerd Font installed (e.g. CaskaydiaCove NF): `icons.ps1` shows every icon, aligned on the ruler; oh-my-posh or starship prompt icons render
- [ ] `unicode.ps1` is unchanged (CJK, emoji, box drawing)
- [ ] `FontWeight` is not exposed in the gallery yet: checked in P4-27
