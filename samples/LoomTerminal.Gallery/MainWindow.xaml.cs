using System.Diagnostics;
using LoomTerminal.Gallery.Diagnostics;
using LoomTerminal.Presentation.Input;
using LoomTerminal.Presentation.Rendering;
using LoomTerminal.Presentation.Session;
using LoomTerminal.Presentation.Theming;
using LoomTerminal.Pty;
using LoomTerminal.WinUI.Controls;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Media;

namespace LoomTerminal.Gallery;

/// <summary>The main window of the gallery: one tab per shell, a settings pane and a status bar.</summary>
/// <remarks>Threading: UI thread only.</remarks>
public sealed partial class MainWindow : Window
{
    private const int KeyCodeT = 84;

    private static readonly TerminalTheme LightThemeColors =
        new() { Foreground = "#1f1f1f", Background = "#ffffff", Cursor = "#1f1f1f", SelectionBackground = "#add6ff" };

    private static readonly TerminalTheme DarkThemeColors =
        new() { Foreground = "#cccccc", Background = "#1e1e1e", Cursor = "#aeafad", SelectionBackground = "#264f78" };

    // The default scheme of Windows Terminal ("Campbell").
    private static readonly TerminalTheme CampbellThemeColors = new()
    {
        Foreground = "#cccccc", Background = "#0c0c0c", Cursor = "#ffffff", SelectionBackground = "#ffffff40",
        Black = "#0c0c0c", Red = "#c50f1f", Green = "#13a10e", Yellow = "#c19c00",
        Blue = "#0037da", Magenta = "#881798", Cyan = "#3a96dd", White = "#cccccc",
        BrightBlack = "#767676", BrightRed = "#e74856", BrightGreen = "#16c60c", BrightYellow = "#f9f1a5",
        BrightBlue = "#3b78ff", BrightMagenta = "#b4009e", BrightCyan = "#61d6d6", BrightWhite = "#f2f2f2",
    };

    private const int MaxIntervals = 3600;

    private readonly DispatcherQueueTimer _statusTimer;
    private readonly List<double> _intervals = [];
    private long _lastTick;
    private TerminalFont? _windowsTerminalFont;
    private ShellInfo? _lastShell;

    /// <summary>Creates the window and opens a tab for the first detected shell.</summary>
    public MainWindow()
    {
        InitializeComponent();
        AddTab(Shells.Detect() is [var first, ..] ? first : new ShellInfo("cmd", "Command Prompt", "cmd.exe"));

        _statusTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _statusTimer.Interval = TimeSpan.FromMilliseconds(250);
        _statusTimer.Tick += (_, _) => UpdateStatus();
        _statusTimer.Start();

        // The window owns the sessions: closing it kills every shell.
        Closed += (_, _) =>
        {
            _statusTimer.Stop();
            CompositionTarget.Rendering -= OnRendering;
            foreach (var tab in Tabs.TabItems.OfType<TabViewItem>().ToList())
            {
                CloseTab(tab);
            }
        };
    }

    private TerminalControl? CurrentTerminal => (Tabs.SelectedItem as TabViewItem)?.Content as TerminalControl;

    private void AddTab(ShellInfo shell)
    {
        _lastShell = shell;
        var terminal = new TerminalControl();
        var tab = new TabViewItem { Header = shell.Name, Content = terminal };
        terminal.TitleChanged += (_, title) =>
        {
            tab.Header = TabHeader(shell, title);
            ToolTipService.SetToolTip(tab, string.IsNullOrWhiteSpace(title) ? shell.Name : $"{shell.Name} — {title}");
        };
        terminal.Exited += (_, code) => tab.Header = $"{shell.Name} [exited {code}]";
        terminal.KeyPreview += (_, e) =>
        {
            // Ctrl+Shift+T opens a new tab with the same shell (exercises KeyPreview).
            if (e.Key is { Ctrl: true, Shift: true, KeyCode: KeyCodeT, Type: TerminalKeyEventType.KeyDown })
            {
                e.Handled = true;
                AddTab(shell);
            }
        };
        ApplySettings(terminal);
        terminal.Session = TerminalSession.Start(new PtyStartOptions(shell.CommandLine));
        Tabs.TabItems.Add(tab);
        Tabs.SelectedItem = tab;
        _ = SetIconAsync(tab, shell.ExecutablePath);
    }

    private static async Task SetIconAsync(TabViewItem tab, string? executablePath)
    {
        if (await ShellIcons.LoadAsync(executablePath) is { } icon)
        {
            tab.IconSource = new ImageIconSource { ImageSource = icon };
        }
    }

    // ConPTY reports the executable path as the first title: keep the friendly name until the shell says more.
    private static string TabHeader(ShellInfo shell, string? title)
    {
        if (string.IsNullOrWhiteSpace(title)
            || (Path.IsPathFullyQualified(title) && title.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)))
        {
            return shell.Name;
        }

        return title;
    }

    // Closing a tab disposes its session, which kills the shell.
    private void CloseTab(TabViewItem tab)
    {
        Tabs.TabItems.Remove(tab);
        if (tab.Content is TerminalControl { Session: { } session } terminal)
        {
            terminal.Session = null;
            session.Dispose();
        }
    }

    private void OnTabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        CloseTab((TabViewItem)args.Tab);
        if (sender.TabItems.Count == 0)
        {
            Close();
        }
    }

    private void OnNewTabClick(object sender, RoutedEventArgs e)
    {
        if (_lastShell is { } shell)
        {
            AddTab(shell);
        }
    }

    // Anchored to the whole button group, like the menu of Windows Terminal; the list follows the installed shells.
    private void OnShellListClick(object sender, RoutedEventArgs e)
    {
        var menu = new MenuFlyout { Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.BottomEdgeAlignedLeft };
        foreach (var shell in Shells.Detect())
        {
            var item = new MenuFlyoutItem { Text = shell.Name };
            item.Click += (_, _) => AddTab(shell);
            menu.Items.Add(item);
        }

        menu.ShowAt(NewTabGroup);
    }

    private void OnTabSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateStatus();
        // The terminal takes the keyboard as soon as its tab is shown; the content is not loaded yet at this point.
        DispatcherQueue.TryEnqueue(() => CurrentTerminal?.Focus(FocusState.Programmatic));
    }

    private void OnSettingChanged(object sender, object e)
    {
        // Raised while InitializeComponent builds the page, before every control exists.
        if (Tabs is null || BlinkSwitch is null || WindowsTerminalFontSwitch is null)
        {
            return;
        }

        foreach (var tab in Tabs.TabItems.OfType<TabViewItem>())
        {
            ApplySettings((TerminalControl)tab.Content);
        }
    }

    // The control does not follow the app theme itself: the host sets Theme.
    private void ApplySettings(TerminalControl terminal)
    {
        var theme = ThemeBox.SelectedIndex switch
        {
            1 => LightThemeColors,
            2 => CampbellThemeColors,
            _ => DarkThemeColors,
        };
        // An invalid color means "use the default" (TerminalTheme), so a half-typed value is harmless.
        terminal.Theme = string.IsNullOrWhiteSpace(BackgroundBox.Text)
            ? theme
            : theme with { Background = BackgroundBox.Text.Trim() };
        if (_windowsTerminalFont is { } font)
        {
            terminal.FontFamily = new FontFamily(string.Join(", ", font.Families));
            terminal.FontSize = font.Size;
            terminal.FontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)font.Weight };
        }
        else
        {
            terminal.FontFamily = new FontFamily((string)FontFamilyBox.SelectedItem);
            terminal.FontSize = FontSizeSlider.Value * 96 / 72;
            terminal.FontWeight = FontWeights.Normal;
        }

        terminal.LineHeight = LineHeightSlider.Value;
        terminal.CursorStyle = (TerminalCursorStyle)CursorStyleBox.SelectedIndex;
        terminal.CursorBlink = BlinkSwitch.IsOn;
    }

    // The settings of Windows Terminal are read only when the user asks for them, once per switch-on.
    private void OnWindowsTerminalFontToggled(object sender, RoutedEventArgs e)
    {
        _windowsTerminalFont = WindowsTerminalFontSwitch.IsOn ? WindowsTerminalFont.Read() : null;
        FontFamilyBox.IsEnabled = FontSizeSlider.IsEnabled = !WindowsTerminalFontSwitch.IsOn;
        OnSettingChanged(sender, e);
    }

    // Subscribing keeps the compositor ticking at 60 Hz, which distorts idle CPU: off by default.
    private void OnMeasureToggled(object sender, RoutedEventArgs e)
    {
        _intervals.Clear();
        _lastTick = 0;
        if (MeasureSwitch.IsOn)
        {
            CompositionTarget.Rendering += OnRendering;
        }
        else
        {
            CompositionTarget.Rendering -= OnRendering;
        }
    }

    private void OnRendering(object? sender, object e)
    {
        long now = Stopwatch.GetTimestamp();
        if (_lastTick != 0)
        {
            if (_intervals.Count == MaxIntervals)
            {
                _intervals.RemoveAt(0);
            }

            _intervals.Add(Stopwatch.GetElapsedTime(_lastTick, now).TotalMilliseconds);
        }

        _lastTick = now;
    }

    private void UpdateStatus()
    {
        if (CurrentTerminal is not { } t)
        {
            StatusText.Text = string.Empty;
            return;
        }

        string text = $"{t.Columns}×{t.Rows}   frame {t.LastFrameTime.TotalMilliseconds:F1} ms";
        StatusText.Text = MeasureSwitch.IsOn ? $"{text}   {FrameStatistics.Compute(_intervals).Format()}" : text;
    }
}
