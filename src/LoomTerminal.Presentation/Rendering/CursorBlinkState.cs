// Ported from xtermjs/xterm.js: addons/addon-webgl/src/CursorBlinkStateManager.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

namespace LoomTerminal.Presentation.Rendering;

/// <summary>
/// The blink phase of the cursor. Instead of the timers and animation frames of xterm.js it is a function of time: the
/// cursor is visible for one interval after a (re)start, hidden for the next, and so on. The backend asks
/// <see cref="IsVisible"/> each frame and schedules a frame at <see cref="NextChange"/>.
/// </summary>
/// <remarks>Threading: thread-safe; the UI thread restarts it while the parser thread reads it.</remarks>
internal sealed class CursorBlinkState
{
    /// <summary>The time between cursor blinks (<c>BLINK_INTERVAL</c> in xterm.js).</summary>
    internal static readonly TimeSpan BlinkInterval = TimeSpan.FromMilliseconds(600);

    private readonly TimeProvider _time;
    private readonly Lock _lock = new();
    private long _origin;
    private bool _paused;

    /// <summary>Creates a blinking cursor that starts visible now.</summary>
    /// <param name="time">The clock.</param>
    internal CursorBlinkState(TimeProvider time)
    {
        _time = time;
        _origin = time.GetTimestamp();
    }

    /// <summary>When the visibility will change, from now; <see langword="null"/> while paused (the cursor stays visible).</summary>
    internal TimeSpan? NextChange
    {
        get
        {
            lock (_lock)
            {
                if (_paused)
                {
                    return null;
                }

                var elapsed = _time.GetElapsedTime(_origin);
                return BlinkInterval - TimeSpan.FromTicks(elapsed.Ticks % BlinkInterval.Ticks);
            }
        }
    }

    /// <summary>Whether the cursor is visible at <paramref name="timestamp"/> (from <see cref="TimeProvider.GetTimestamp"/>).</summary>
    /// <param name="timestamp">The moment to evaluate.</param>
    /// <returns><see langword="true"/> when visible; always while paused.</returns>
    internal bool IsVisible(long timestamp)
    {
        lock (_lock)
        {
            if (_paused)
            {
                return true;
            }

            var elapsed = _time.GetElapsedTime(_origin, timestamp);
            return elapsed < TimeSpan.Zero || (elapsed.Ticks / BlinkInterval.Ticks) % 2 == 0;
        }
    }

    /// <summary>Makes the cursor visible now and restarts the cycle (on input). Ignored while paused.</summary>
    internal void Restart()
    {
        lock (_lock)
        {
            if (!_paused)
            {
                _origin = _time.GetTimestamp();
            }
        }
    }

    /// <summary>Stops blinking: the cursor stays visible (focus lost).</summary>
    internal void Pause()
    {
        lock (_lock)
        {
            _paused = true;
        }
    }

    /// <summary>Starts blinking again from a visible cursor (focus gained).</summary>
    internal void Resume()
    {
        lock (_lock)
        {
            _paused = false;
            _origin = _time.GetTimestamp();
        }
    }
}
