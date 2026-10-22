// Inspired by LoomEditor: src/LoomEditor.WinUI/Input/ImeInput.cs and ScreenCoordinates.cs
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Windows.Foundation;
using Windows.UI.Text.Core;

namespace LoomTerminal.WinUI.Input;

/// <summary>
/// Types with an IME, the emoji panel and other Windows text services through a <see cref="CoreTextEditContext"/>.
/// The edit context holds only the current composition: the terminal is not a text document. Nothing is sent to the
/// shell until the composition completes (xterm.js <c>CompositionHelper</c>).
/// </summary>
/// <remarks>
/// Windows 11 only (microsoft-ui-xaml #4239): <see cref="TryCreate"/> returns <see langword="null"/> elsewhere.
/// Threading: UI thread only.
/// </remarks>
internal sealed partial class ImeTextInput : IDisposable
{
    private readonly CoreTextEditContext _editContext;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly Action<string?> _compositionChanged;
    private readonly Action<string> _commit;
    private readonly Func<(Rect Text, Rect Control)?> _getLayoutBounds;
    private string _text = string.Empty;
    private int _selectionEnd;
    private bool _disposed;

    private ImeTextInput(
        CoreTextEditContext editContext, DispatcherQueue dispatcherQueue, Action<string?> compositionChanged, Action<string> commit,
        Func<(Rect Text, Rect Control)?> getLayoutBounds)
    {
        _editContext = editContext;
        _dispatcherQueue = dispatcherQueue;
        _compositionChanged = compositionChanged;
        _commit = commit;
        _getLayoutBounds = getLayoutBounds;

        _editContext.InputScope = CoreTextInputScope.Default;
        _editContext.InputPaneDisplayPolicy = CoreTextInputPaneDisplayPolicy.Manual;
        _editContext.TextRequested += OnTextRequested;
        _editContext.SelectionRequested += OnSelectionRequested;
        _editContext.TextUpdating += OnTextUpdating;
        _editContext.SelectionUpdating += OnSelectionUpdating;
        _editContext.LayoutRequested += OnLayoutRequested;
        _editContext.CompositionStarted += OnCompositionStarted;
        _editContext.CompositionCompleted += OnCompositionCompleted;
    }

    /// <summary>Whether a composition is active; keys and characters belong to the IME meanwhile.</summary>
    public bool IsComposing { get; private set; }

    /// <summary>Creates the IME input, or returns <see langword="null"/> before Windows 11 or when the text services fail.</summary>
    /// <param name="dispatcherQueue">The UI thread queue.</param>
    /// <param name="compositionChanged">Called with the composition to draw, or <see langword="null"/> when it ended.</param>
    /// <param name="commit">Called with the committed text.</param>
    /// <param name="getLayoutBounds">The cursor cell and the control, in screen pixels (see <see cref="ToScreenPixels"/>).</param>
    /// <returns>The input, or <see langword="null"/>.</returns>
#pragma warning disable CA1031 // Whatever the failure is, the terminal keeps working without IME.
    public static ImeTextInput? TryCreate(
        DispatcherQueue dispatcherQueue, Action<string?> compositionChanged, Action<string> commit, Func<(Rect Text, Rect Control)?> getLayoutBounds)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            return null;
        }

        try
        {
            var manager = CoreTextServicesManager.GetForCurrentView();
            return new ImeTextInput(manager.CreateEditContext(), dispatcherQueue, compositionChanged, commit, getLayoutBounds);
        }
        catch (Exception)
        {
            return null;
        }
    }
#pragma warning restore CA1031

    /// <summary>Tells the text services that the control got the focus.</summary>
    public void NotifyFocusEnter() => _editContext.NotifyFocusEnter();

    /// <summary>Converts a rectangle of an element (DIPs) to screen pixels, which is what the IME wants.</summary>
    /// <param name="element">The element.</param>
    /// <param name="rect">The rectangle in the element's coordinates.</param>
    /// <returns>The rectangle, or <see langword="null"/> when the element is not in a window.</returns>
    public static Rect? ToScreenPixels(UIElement element, Rect rect)
    {
        if (element.XamlRoot is not { } root)
        {
            return null;
        }

        var inContent = element.TransformToVisual(null).TransformBounds(rect);
        var scale = root.RasterizationScale;
        var hwnd = Win32Interop.GetWindowFromWindowId(root.ContentIslandEnvironment.AppWindowId);
        var origin = default(NativePoint);
        if (!ClientToScreen(hwnd, ref origin))
        {
            return null;
        }

        return new Rect((inContent.X * scale) + origin.X, (inContent.Y * scale) + origin.Y, inContent.Width * scale, inContent.Height * scale);
    }

    /// <summary>Tells the text services that the cursor moved, so the candidate window follows. Queued.</summary>
    public void RequestLayoutChanged()
    {
        if (IsComposing)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                if (!_disposed)
                {
                    _editContext.NotifyLayoutChanged();
                }
            });
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _editContext.TextRequested -= OnTextRequested;
        _editContext.SelectionRequested -= OnSelectionRequested;
        _editContext.TextUpdating -= OnTextUpdating;
        _editContext.SelectionUpdating -= OnSelectionUpdating;
        _editContext.LayoutRequested -= OnLayoutRequested;
        _editContext.CompositionStarted -= OnCompositionStarted;
        _editContext.CompositionCompleted -= OnCompositionCompleted;
        _editContext.NotifyFocusLeave();
        if (IsComposing || _text.Length > 0)
        {
            // The composition is abandoned, not committed.
            IsComposing = false;
            _compositionChanged(null);
        }
    }

    private void OnTextRequested(CoreTextEditContext sender, CoreTextTextRequestedEventArgs args)
    {
        var start = Math.Clamp(args.Request.Range.StartCaretPosition, 0, _text.Length);
        var end = Math.Clamp(args.Request.Range.EndCaretPosition, start, _text.Length);
        args.Request.Text = _text[start..end];
    }

    private void OnSelectionRequested(CoreTextEditContext sender, CoreTextSelectionRequestedEventArgs args) =>
        args.Request.Selection = new CoreTextRange { StartCaretPosition = _selectionEnd, EndCaretPosition = _selectionEnd };

    // The emoji panel sends updates without CompositionStarted: that text is final.
    private void OnTextUpdating(CoreTextEditContext sender, CoreTextTextUpdatingEventArgs args)
    {
        args.Result = CoreTextTextUpdatingResult.Succeeded;
        if (!IsComposing)
        {
            if (args.Text.Length > 0)
            {
                _commit(args.Text);
            }

            return;
        }

        var start = Math.Clamp(args.Range.StartCaretPosition, 0, _text.Length);
        var end = Math.Clamp(args.Range.EndCaretPosition, start, _text.Length);
        _text = string.Concat(_text.AsSpan(0, start), args.Text, _text.AsSpan(end));
        _selectionEnd = Math.Clamp(args.NewSelection.EndCaretPosition, 0, _text.Length);
        _compositionChanged(_text);
        RequestLayoutChanged();
    }

    private void OnSelectionUpdating(CoreTextEditContext sender, CoreTextSelectionUpdatingEventArgs args)
    {
        args.Result = CoreTextSelectionUpdatingResult.Succeeded;
        _selectionEnd = Math.Clamp(args.Selection.EndCaretPosition, 0, _text.Length);
    }

    // Screen pixels, LayoutBounds only (the visual-pixels properties crash the app, LoomEditor P3-48).
    private void OnLayoutRequested(CoreTextEditContext sender, CoreTextLayoutRequestedEventArgs args)
    {
        if (_getLayoutBounds() is { } bounds)
        {
            args.Request.LayoutBounds.TextBounds = bounds.Text;
            args.Request.LayoutBounds.ControlBounds = bounds.Control;
        }
    }

    private void OnCompositionStarted(CoreTextEditContext sender, CoreTextCompositionStartedEventArgs args)
    {
        IsComposing = true;
        _text = string.Empty;
        _selectionEnd = 0;
    }

    // An Escape ends with an empty text: nothing is sent.
    private void OnCompositionCompleted(CoreTextEditContext sender, CoreTextCompositionCompletedEventArgs args)
    {
        var text = _text;
        IsComposing = false;
        _text = string.Empty;
        _selectionEnd = 0;
        _compositionChanged(null);
        if (text.Length > 0)
        {
            _commit(text);
        }

        // The edit context is empty again: tell the text services, outside of their handler.
        _dispatcherQueue.TryEnqueue(() =>
        {
            if (!_disposed)
            {
                _editContext.NotifyTextChanged(
                    new CoreTextRange { StartCaretPosition = 0, EndCaretPosition = text.Length }, 0, new CoreTextRange());
            }
        });
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [LibraryImport("user32.dll", EntryPoint = "ClientToScreen")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ClientToScreen(nint hwnd, ref NativePoint point);
}
