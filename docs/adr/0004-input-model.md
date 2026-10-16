# ADR 0004: Keyboard and mouse input model

- Status: accepted (2026-10-16)
- Date: 2026-10-16

## Context

xterm.js turns DOM `KeyboardEvent`s into VT sequences in `src/common/input/Keyboard.ts`
(`evaluateKeyboardEvent`, ~330 lines of rules keyed on DOM `keyCode`), with a thorough test suite. Our backends
receive `Windows.System.VirtualKey` (WinUI) and `Avalonia.Input.Key` (Avalonia).

## Decisions

1. `LoomTerminal.Presentation.Input.TerminalKeyEvent` is our own record: `KeyCode` (int, **DOM `keyCode` values**),
   `Key` (string, DOM `key` value: the produced character or a name such as `"ArrowUp"`), `Ctrl`, `Alt`, `Shift`,
   `Meta`, `AltGraph`, `Type` (down / press).
2. Because the values are DOM's, `Keyboard.ts` and `Keyboard.test.ts` are **ported verbatim** (P3-03). Each
   backend owns a small mapping table `VirtualKey → keyCode` / `Avalonia Key → keyCode` (Windows virtual-key codes
   and DOM keyCodes are mostly identical, which makes the WinUI table short).
3. Mouse: `TerminalPointerEvent` (cell column/row, pixel position, button, action, modifiers) feeds the ported
   `CoreMouseService` (P1-35), which owns the protocols (X10, VT200, DRAG, ANY) and encodings (DEFAULT, SGR,
   SGR_PIXELS).
4. The host can intercept keys before the terminal: Presentation raises a cancellable `KeyPreview` event (VIGO uses
   it for its Ctrl+Shift shortcut table). AltGr combinations are never offered as shortcuts.
