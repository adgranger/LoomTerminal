using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using LoomTerminal.Avalonia.Controls;
using LoomTerminal.Presentation.Links;
using LoomTerminal.Presentation.Rendering;
using LoomTerminal.Presentation.Session;
using LoomTerminal.Presentation.Theming;
using LoomTerminal.Pty;

namespace LoomTerminal.AvaloniaGallery;

/// <summary>The main window of the gallery: one tab per shell, a settings pane and a status bar.</summary>
/// <remarks>Threading: UI thread only.</remarks>
public sealed partial class MainWindow : Window, ILinkHandler
{
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

    private readonly List<TabStripItem> _tabs = [];
    private ShellInfo? _lastShell;
    private TerminalFont? _windowsTerminalFont;

    /// <summary>Creates the window and opens a tab for the first detected shell.</summary>
    public MainWindow()
    {
        InitializeComponent();
        AddTab(Shells.Detect() is [var first, ..] ? first : new ShellInfo("cmd", "Command Prompt", "cmd.exe"));

        var statusTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        statusTimer.Tick += (_, _) => UpdateStatus();
        statusTimer.Start();

        // Ctrl+Shift+T opens a new tab with the same shell; tunnelled so the terminal never sees it.
        AddHandler(KeyDownEvent, OnWindowKeyDown, RoutingStrategies.Tunnel);

        // The window owns the sessions: closing it kills every shell.
        Closed += (_, _) =>
        {
            statusTimer.Stop();
            foreach (var tab in _tabs.ToList())
            {
                CloseTab(tab);
            }
        };
    }

    private TerminalView? CurrentTerminal => (Tabs.SelectedItem as TabStripItem)?.Tag as TerminalView;

    // Raised on the parser thread: only https links reach it, opening them is up to the host.
    bool ILinkHandler.CanOpen(Uri uri) => uri.Scheme == Uri.UriSchemeHttps;

    void ILinkHandler.Open(Uri uri) => Dispatcher.UIThread.Post(() => _ = GetTopLevel(this)?.Launcher.LaunchUriAsync(uri));

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (e is { Key: Key.T, KeyModifiers: KeyModifiers.Control | KeyModifiers.Shift } && _lastShell is { } shell)
        {
            e.Handled = true;
            AddTab(shell);
        }
    }

    private void AddTab(ShellInfo shell)
    {
        _lastShell = shell;
        var terminal = new TerminalView();
        var label = new TextBlock { Text = shell.Name, VerticalAlignment = VerticalAlignment.Center };
        var close = new Button
        {
            Content = "✕", FontSize = 11, Padding = new(6, 4), CornerRadius = new(4), Background = Brushes.Transparent, BorderThickness = new(0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        if (ShellIcons.Load(shell.ExecutablePath) is { } icon)
        {
            var image = new Image
            {
                Source = icon, Width = 14, Height = 14, Stretch = Stretch.Uniform, VerticalAlignment = VerticalAlignment.Center,
            };
            RenderOptions.SetBitmapInterpolationMode(image, BitmapInterpolationMode.HighQuality);
            header.Children.Add(image);
        }

        header.Children.Add(label);
        header.Children.Add(close);
        var tab = new TabStripItem { Content = header, Tag = terminal };
        ToolTip.SetTip(tab, shell.Name);
        close.Click += (_, _) => OnTabCloseRequested(tab);
        terminal.TitleChanged += (_, title) =>
        {
            label.Text = TabHeader(shell, title);
            ToolTip.SetTip(tab, string.IsNullOrWhiteSpace(title) ? shell.Name : $"{shell.Name} — {title}");
        };
        terminal.Exited += (_, code) => label.Text = $"{shell.Name} [exited {code}]";
        ApplySettings(terminal);
        var session = TerminalSession.Start(
            new PtyStartOptions(shell.CommandLine), new TerminalSessionOptions { Scrollback = 10_000 });
        session.LinkHandler = this;
        terminal.Session = session;
        _tabs.Add(tab);
        Tabs.Items.Add(tab);
        Tabs.SelectedItem = tab;
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
    private void CloseTab(TabStripItem tab)
    {
        _tabs.Remove(tab);
        Tabs.Items.Remove(tab);
        if (tab.Tag is TerminalView { Session: { } session } terminal)
        {
            terminal.Session = null;
            session.Dispose();
        }
    }

    private void OnTabCloseRequested(TabStripItem tab)
    {
        CloseTab(tab);
        if (_tabs.Count == 0)
        {
            Close();
        }
    }

    private void OnNewTabClick(object? sender, RoutedEventArgs e)
    {
        if (_lastShell is { } shell)
        {
            AddTab(shell);
        }
    }

    // Anchored to the whole button group, like the menu of Windows Terminal; the list follows the installed shells.
    private void OnShellListClick(object? sender, RoutedEventArgs e)
    {
        var menu = new MenuFlyout { Placement = PlacementMode.BottomEdgeAlignedLeft };
        foreach (var shell in Shells.Detect())
        {
            var item = new MenuItem { Header = shell.Name };
            item.Click += (_, _) => AddTab(shell);
            menu.Items.Add(item);
        }

        menu.ShowAt(NewTabGroup);
    }

    // The content is swapped, not kept: the terminal detaches from the visual tree and attaches again on a return.
    private void OnTabSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        TerminalHost.Content = CurrentTerminal;
        UpdateStatus();
        Dispatcher.UIThread.Post(() => CurrentTerminal?.Focus());
    }

    private void OnSettingChanged(object? sender, RoutedEventArgs e)
    {
        // Raised while InitializeComponent builds the page, before every control exists.
        if (BlinkSwitch is null || WindowsTerminalFontSwitch is null)
        {
            return;
        }

        foreach (var tab in _tabs)
        {
            ApplySettings((TerminalView)tab.Tag!);
        }
    }

    // The control does not follow the app theme itself: the host sets TerminalTheme.
    private void ApplySettings(TerminalView terminal)
    {
        var theme = ThemeBox.SelectedIndex switch
        {
            1 => LightThemeColors,
            2 => CampbellThemeColors,
            _ => DarkThemeColors,
        };
        // An invalid color means "use the default" (TerminalTheme), so a half-typed value is harmless.
        terminal.TerminalTheme = string.IsNullOrWhiteSpace(BackgroundBox.Text)
            ? theme
            : theme with { Background = BackgroundBox.Text.Trim() };
        if (_windowsTerminalFont is { } font)
        {
            terminal.FontFamily = new FontFamily(string.Join(", ", font.Families));
            terminal.FontSize = font.Size;
            terminal.FontWeight = (FontWeight)font.Weight;
        }
        else
        {
            terminal.FontFamily = new FontFamily((string)((ComboBoxItem)FontFamilyBox.SelectedItem!).Content!);
            terminal.FontSize = FontSizeSlider.Value * 96 / 72;
            terminal.FontWeight = FontWeight.Normal;
        }

        terminal.LineHeight = LineHeightSlider.Value;
        terminal.CursorStyle = (TerminalCursorStyle)CursorStyleBox.SelectedIndex;
        terminal.CursorBlink = BlinkSwitch.IsChecked == true;
    }

    // The settings of Windows Terminal are read only when the user asks for them, once per switch-on.
    private void OnWindowsTerminalFontToggled(object? sender, RoutedEventArgs e)
    {
        bool on = WindowsTerminalFontSwitch.IsChecked == true;
        _windowsTerminalFont = on ? WindowsTerminalFont.Read() : null;
        FontFamilyBox.IsEnabled = FontSizeSlider.IsEnabled = !on;
        OnSettingChanged(sender, e);
    }

    private void UpdateStatus() =>
        StatusText.Text = CurrentTerminal is { } t ? $"{t.Columns}×{t.Rows}" : string.Empty;
}
