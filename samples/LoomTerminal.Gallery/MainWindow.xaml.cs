using LoomTerminal.Presentation.Input;
using LoomTerminal.Presentation.Session;
using LoomTerminal.Presentation.Theming;
using LoomTerminal.Pty;
using LoomTerminal.WinUI.Controls;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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

    private readonly DispatcherQueueTimer _statusTimer;

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
            foreach (var tab in Tabs.TabItems.OfType<TabViewItem>().ToList())
            {
                CloseTab(tab);
            }
        };
    }

    private TerminalControl? CurrentTerminal => (Tabs.SelectedItem as TabViewItem)?.Content as TerminalControl;

    private void AddTab(ShellInfo shell)
    {
        var terminal = new TerminalControl();
        var tab = new TabViewItem { Header = shell.Name, Content = terminal };
        terminal.TitleChanged += (_, title) => tab.Header = title;
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

    private void OnAddTabButtonClick(TabView sender, object args)
    {
        var menu = new MenuFlyout();
        foreach (var shell in Shells.Detect())
        {
            var item = new MenuFlyoutItem { Text = shell.Name };
            item.Click += (_, _) => AddTab(shell);
            menu.Items.Add(item);
        }

        menu.ShowAt(sender);
    }

    private void OnTabSelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateStatus();

    private void OnSettingChanged(object sender, object e)
    {
        // Raised while InitializeComponent builds the page, before every control exists.
        if (Tabs is null || BlinkSwitch is null)
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
        terminal.Theme = LightTheme.IsOn ? LightThemeColors : DarkThemeColors;
        terminal.FontFamily = new FontFamily((string)FontFamilyBox.SelectedItem);
        terminal.FontSize = FontSizeSlider.Value;
        terminal.LineHeight = LineHeightSlider.Value;
        terminal.CursorStyle = (TerminalCursorStyle)CursorStyleBox.SelectedIndex;
        terminal.CursorBlink = BlinkSwitch.IsOn;
    }

    private void UpdateStatus() =>
        StatusText.Text = CurrentTerminal is { } t
            ? $"{t.Columns}×{t.Rows}   frame {t.LastFrameTime.TotalMilliseconds:F1} ms"
            : string.Empty;
}
