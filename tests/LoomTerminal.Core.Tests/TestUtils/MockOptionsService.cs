// Ported from xtermjs/xterm.js: src/common/TestUtils.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Common;
using LoomTerminal.Core.Services;

namespace LoomTerminal.Core.Tests.TestUtils;

/// <summary>
/// An options service without validation. <see cref="RawOptions"/> can be assigned, like the JS tests assign
/// <c>options.rawOptions.x</c>: <c>options.RawOptions = options.RawOptions with { ConvertEol = true };</c>.
/// Assigning it raises <see cref="OnOptionChange"/> for each changed property.
/// </summary>
/// <remarks>Threading: not thread-safe; used from a single test thread.</remarks>
internal sealed class MockOptionsService : IOptionsService, IDisposable
{
    private readonly Emitter<TerminalOption> _onOptionChange = new();
    private TerminalOptions _rawOptions;

    public MockOptionsService(TerminalOptions? testOptions = null) => _rawOptions = testOptions ?? TerminalOptions.Default;

    public TerminalOptions RawOptions
    {
        get => _rawOptions;
        set
        {
            TerminalOptions old = _rawOptions;
            _rawOptions = value;
            foreach (TerminalOption key in OptionsService.Diff(old, value))
            {
                _onOptionChange.Fire(key);
            }
        }
    }

    public TerminalOptions Options => RawOptions;

    public void Dispose() => _onOptionChange.Dispose();

    public IEvent<TerminalOption> OnOptionChange => _onOptionChange.Event;

    public void SetOptions(TerminalOptions value) => RawOptions = value;

    public IDisposable OnSpecificOptionChange(TerminalOption key, Action listener) =>
        _onOptionChange.Event.Subscribe(eventKey =>
        {
            if (eventKey == key)
            {
                listener();
            }
        });

    public IDisposable OnMultipleOptionChange(IReadOnlyList<TerminalOption> keys, Action listener) =>
        _onOptionChange.Event.Subscribe(eventKey =>
        {
            if (keys.Contains(eventKey))
            {
                listener();
            }
        });
}
