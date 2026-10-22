// Inspired by LoomEditor: src/LoomEditor.WinUI/Controls/AnimationFrameScheduler.cs
using Microsoft.UI.Xaml.Media;

namespace LoomTerminal.WinUI.Controls;

/// <summary>Runs one callback at the next composition frame, however many times a frame was requested.</summary>
/// <remarks>
/// Threading: UI thread only. <see cref="CompositionTarget.Rendering"/> is subscribed only while a frame is pending,
/// so an idle terminal costs no CPU.
/// </remarks>
internal sealed class FrameScheduler(Action onFrame)
{
    private bool _isSubscribed;

    /// <summary>Asks for the callback at the next frame; a no-op when one is already pending.</summary>
    public void Request()
    {
        if (_isSubscribed)
        {
            return;
        }

        CompositionTarget.Rendering += OnRendering;
        _isSubscribed = true;
    }

    /// <summary>Drops a pending request.</summary>
    public void Cancel()
    {
        if (_isSubscribed)
        {
            CompositionTarget.Rendering -= OnRendering;
            _isSubscribed = false;
        }
    }

    private void OnRendering(object? sender, object e)
    {
        // Unsubscribe first: the callback may request the following frame.
        Cancel();
        onFrame();
    }
}
