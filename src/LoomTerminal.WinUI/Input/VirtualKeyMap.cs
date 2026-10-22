// Inspired by LoomEditor: src/LoomEditor.WinUI/Input/KeyboardInput.cs (InputKeyboardSource key state)
using System.Runtime.InteropServices;
using Windows.System;

namespace LoomTerminal.WinUI.Input;

/// <summary>
/// Maps WinUI virtual keys to the DOM values <c>Keyboard.ts</c> expects (ADR 0004): <c>keyCode</c> is the Windows
/// virtual-key code itself, <c>key</c> is a DOM name for non-printable keys and the layout's character otherwise.
/// </summary>
/// <remarks>Threading: stateless; UI thread (reads the keyboard layout and state of the calling thread).</remarks>
internal static partial class VirtualKeyMap
{
    private const uint DoNotChangeKeyboardState = 0x4;
    private const uint MapVkToVsc = 0;

    /// <summary>DOM <c>keyCode</c> of <paramref name="key"/>: browsers on Windows report the virtual-key code.</summary>
    /// <param name="key">The key.</param>
    /// <returns>The key code.</returns>
    public static int ToKeyCode(VirtualKey key) => (int)key;

    /// <summary>Whether <paramref name="key"/> is a Tab or an arrow: keys WinUI focus navigation would steal.</summary>
    /// <param name="key">The key.</param>
    /// <returns>True for Tab and the four arrows.</returns>
    public static bool IsNavigationKey(VirtualKey key) =>
        key is VirtualKey.Tab or VirtualKey.Up or VirtualKey.Down or VirtualKey.Left or VirtualKey.Right;

    /// <summary>DOM <c>key</c> name of a non-printable key, or <see langword="null"/> when the key prints.</summary>
    /// <param name="key">The key.</param>
    /// <returns>The DOM name.</returns>
    public static string? NonPrintableName(VirtualKey key)
    {
        if (key is >= VirtualKey.F1 and <= VirtualKey.F24)
        {
            return "F" + (key - VirtualKey.F1 + 1);
        }

        return key switch
        {
            VirtualKey.Back => "Backspace",
            VirtualKey.Tab => "Tab",
            VirtualKey.Enter => "Enter",
            VirtualKey.Shift or VirtualKey.LeftShift or VirtualKey.RightShift => "Shift",
            VirtualKey.Control or VirtualKey.LeftControl or VirtualKey.RightControl => "Control",
            VirtualKey.Menu or VirtualKey.LeftMenu or VirtualKey.RightMenu => "Alt",
            VirtualKey.Pause => "Pause",
            VirtualKey.CapitalLock => "CapsLock",
            VirtualKey.Escape => "Escape",
            VirtualKey.PageUp => "PageUp",
            VirtualKey.PageDown => "PageDown",
            VirtualKey.End => "End",
            VirtualKey.Home => "Home",
            VirtualKey.Left => "ArrowLeft",
            VirtualKey.Up => "ArrowUp",
            VirtualKey.Right => "ArrowRight",
            VirtualKey.Down => "ArrowDown",
            VirtualKey.Insert => "Insert",
            VirtualKey.Delete => "Delete",
            VirtualKey.LeftWindows or VirtualKey.RightWindows => "Meta",
            VirtualKey.Application => "ContextMenu",
            VirtualKey.NumberKeyLock => "NumLock",
            VirtualKey.Scroll => "ScrollLock",
            _ => null,
        };
    }

    /// <summary>The DOM <c>key</c> value of <paramref name="key"/> in the current keyboard layout.</summary>
    /// <param name="key">The key.</param>
    /// <param name="altGraph">Whether AltGr is down: Ctrl is then part of the character, otherwise it is dropped.</param>
    /// <returns>A DOM name, the produced character, or an empty string (dead key, or no character).</returns>
    public static unsafe string ToDomKey(VirtualKey key, bool altGraph)
    {
        if (NonPrintableName(key) is { } name)
        {
            return name;
        }

        var layout = GetKeyboardLayout(0);
        Span<byte> state = stackalloc byte[256];
        bool ok;
        fixed (byte* keyState = state)
        {
            ok = GetKeyboardState(keyState);
        }

        if (!ok)
        {
            return string.Empty;
        }

        if (!altGraph)
        {
            // Ctrl+C would give U+0003; DOM key is "c".
            state[(int)VirtualKey.Control] = state[(int)VirtualKey.LeftControl] = state[(int)VirtualKey.RightControl] = 0;
        }

        var scanCode = MapVirtualKeyEx((uint)key, MapVkToVsc, layout);
        Span<char> buffer = stackalloc char[8];
        int count;
        fixed (byte* keyState = state)
        fixed (char* chars = buffer)
        {
            count = ToUnicodeEx((uint)key, scanCode, keyState, chars, buffer.Length, DoNotChangeKeyboardState, layout);
        }

        // count < 0: dead key (accent stored, not yet a character); 0: nothing; control characters are not "keys".
        if (count <= 0 || char.IsControl(buffer[0]))
        {
            return string.Empty;
        }

        return new string(buffer[..count]);
    }

    [LibraryImport("user32.dll")]
    private static partial nint GetKeyboardLayout(uint threadId);

    [LibraryImport("user32.dll", EntryPoint = "MapVirtualKeyExW")]
    private static partial uint MapVirtualKeyEx(uint code, uint mapType, nint layout);

    [LibraryImport("user32.dll", EntryPoint = "GetKeyboardState")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static unsafe partial bool GetKeyboardState(byte* state);

    [LibraryImport("user32.dll", EntryPoint = "ToUnicodeEx")]
    private static unsafe partial int ToUnicodeEx(
        uint virtualKey, uint scanCode, byte* keyState, char* buffer, int bufferSize, uint flags, nint layout);
}
