// Ported from xtermjs/xterm.js: src/common/services/UnicodeService.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Common;
using LoomTerminal.Core.Input;

namespace LoomTerminal.Core.Services;

/// <summary>
/// The Unicode width service. Character properties are packed in an <see cref="int"/>: bit 0 is "should join" with the
/// preceding character, bits 1..2 the width, the other bits the character kind.
/// </summary>
/// <remarks>Threading: not thread-safe; used from the parser thread only.</remarks>
internal sealed class UnicodeService : IUnicodeService, IDisposable
{
    private readonly Dictionary<string, IUnicodeVersionProvider> _providers = [];
    private readonly Emitter<string> _onChange = new();
    private string _active;
    private IUnicodeVersionProvider _activeProvider;

    /// <summary>Creates the service with the Unicode 6 and 11 providers registered, 11 being active.</summary>
    public UnicodeService()
    {
        Register(new UnicodeV6());
        UnicodeV11 defaultProvider = new();
        Register(defaultProvider);
        _active = defaultProvider.Version;
        _activeProvider = defaultProvider;
    }

    /// <inheritdoc />
    public IEvent<string> OnChange => _onChange.Event;

    /// <inheritdoc />
    public string[] Versions => [.. _providers.Keys];

    /// <inheritdoc />
    public string ActiveVersion
    {
        get => _active;
        set
        {
            if (!_providers.TryGetValue(value, out IUnicodeVersionProvider? provider))
            {
                throw new InvalidOperationException($"unknown Unicode version \"{value}\"");
            }

            _active = value;
            _activeProvider = provider;
            _onChange.Fire(value);
        }
    }

    /// <summary>Whether the packed value says the character joins the preceding one.</summary>
    public static bool ExtractShouldJoin(int value) => (value & 1) != 0;

    /// <summary>The width (0, 1 or 2) of a packed value.</summary>
    public static int ExtractWidth(int value) => (value >> 1) & 0x3;

    /// <summary>The character kind of a packed value.</summary>
    public static int ExtractCharKind(int value) => value >> 3;

    /// <summary>Packs a character kind, a width and the "should join" flag.</summary>
    public static int CreatePropertyValue(int state, int width, bool shouldJoin = false) =>
        ((state & 0xffffff) << 3) | ((width & 3) << 1) | (shouldJoin ? 1 : 0);

    /// <summary>Disposes the change event.</summary>
    public void Dispose() => _onChange.Dispose();

    /// <inheritdoc />
    public void Register(IUnicodeVersionProvider provider) => _providers[provider.Version] = provider;

    /// <inheritdoc />
    public int Wcwidth(int codepoint) => _activeProvider.Wcwidth(codepoint);

    /// <inheritdoc />
    public int GetStringCellWidth(string s)
    {
        int result = 0;
        int precedingInfo = 0;
        int length = s.Length;
        for (int i = 0; i < length; ++i)
        {
            int code = s[i];

            // surrogate pair first
            if (0xD800 <= code && code <= 0xDBFF)
            {
                if (++i >= length)
                {
                    // this should not happen with strings retrieved from Buffer.translateToString as it converts from
                    // UTF-32 and therefore always should contain the second part for any other string we still have
                    // to handle it somehow: simply treat the lonely surrogate first as a single char (UCS-2 behavior)
                    return result + Wcwidth(code);
                }

                int second = s[i];

                // convert surrogate pair to high codepoint only for valid second part (UTF-16)
                // otherwise treat them independently (UCS-2 behavior)
                if (0xDC00 <= second && second <= 0xDFFF)
                {
                    code = ((code - 0xD800) * 0x400) + second - 0xDC00 + 0x10000;
                }
                else
                {
                    result += Wcwidth(second);
                }
            }

            int currentInfo = CharProperties(code, precedingInfo);
            int chWidth = ExtractWidth(currentInfo);
            if (ExtractShouldJoin(currentInfo))
            {
                chWidth -= ExtractWidth(precedingInfo);
            }

            result += chWidth;
            precedingInfo = currentInfo;
        }

        return result;
    }

    /// <inheritdoc />
    public int CharProperties(int codepoint, int preceding) => _activeProvider.CharProperties(codepoint, preceding);
}
