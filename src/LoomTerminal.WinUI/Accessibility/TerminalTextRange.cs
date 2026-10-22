using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Automation.Text;

namespace LoomTerminal.WinUI.Accessibility;

/// <summary>A range [start, end) over an immutable snapshot of the terminal text. Units: character, word, line.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed partial class TerminalTextRange : ITextRangeProvider
{
    private readonly string _text;
    private readonly Func<IRawElementProviderSimple> _element;
    private int _start;
    private int _end;

    public TerminalTextRange(string text, Func<IRawElementProviderSimple> element, int start, int end)
    {
        _text = text;
        _element = element;
        (_start, _end) = (start, end);
    }

    public ITextRangeProvider Clone() => new TerminalTextRange(_text, _element, _start, _end);

    public bool Compare(ITextRangeProvider textRangeProvider)
        => textRangeProvider is TerminalTextRange other && other._start == _start && other._end == _end;

    public int CompareEndpoints(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint)
        => Get(endpoint).CompareTo(((TerminalTextRange)targetRange).Get(targetEndpoint));

    public void ExpandToEnclosingUnit(TextUnit unit)
    {
        var b = Boundaries(unit);
        if (b.Length < 2)
        {
            return;
        }

        var i = Math.Min(IndexOfUnit(b, _start), b.Length - 2);
        (_start, _end) = (b[i], b[i + 1]);
    }

    public ITextRangeProvider? FindAttribute(int attributeId, object value, bool backward) => null;

    public ITextRangeProvider? FindText(string text, bool backward, bool ignoreCase)
    {
        if (text.Length == 0)
        {
            return null;
        }

        var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var span = _text.AsSpan(_start, _end - _start);
        var index = backward ? span.LastIndexOf(text, comparison) : span.IndexOf(text, comparison);
        return index < 0 ? null : new TerminalTextRange(_text, _element, _start + index, _start + index + text.Length);
    }

    // Not supported: the terminal exposes no text attributes.
    public object GetAttributeValue(int attributeId) => DependencyProperty.UnsetValue;

    // shortcut: no bounding rectangles (needs cell metrics in the provider); screen readers fall back to the element.
    public void GetBoundingRectangles(out double[] returnValue) => returnValue = [];

    public IRawElementProviderSimple GetEnclosingElement() => _element();

    public string GetText(int maxLength)
    {
        var length = maxLength < 0 ? _end - _start : Math.Min(maxLength, _end - _start);
        return _text.Substring(_start, length);
    }

    public int Move(TextUnit unit, int count)
    {
        var b = Boundaries(unit);
        var last = b.Length - 2;
        if (last < 0)
        {
            return 0;
        }

        var i = Math.Min(IndexOfUnit(b, _start), last);
        var target = Math.Clamp(i + count, 0, last);
        (_start, _end) = (b[target], b[target + 1]);
        return target - i;
    }

    public int MoveEndpointByUnit(TextPatternRangeEndpoint endpoint, TextUnit unit, int count)
    {
        var b = Boundaries(unit);
        var position = Get(endpoint);
        var moved = 0;
        while (moved < Math.Abs(count))
        {
            if (count > 0 ? position >= b[^1] : position <= 0)
            {
                break;
            }

            position = count > 0 ? Array.Find(b, x => x > position) : Array.FindLast(b, x => x < position);
            moved++;
        }

        Set(endpoint, position);
        return count > 0 ? moved : -moved;
    }

    public void MoveEndpointByRange(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint)
        => Set(endpoint, ((TerminalTextRange)targetRange).Get(targetEndpoint));

    // Read only: the terminal selection belongs to the pointer and keyboard.
    public void Select()
    {
    }

    public void AddToSelection()
    {
    }

    public void RemoveFromSelection()
    {
    }

    public void ScrollIntoView(bool alignToTop)
    {
    }

    public IRawElementProviderSimple[] GetChildren() => [];

    private int Get(TextPatternRangeEndpoint endpoint) => endpoint == TextPatternRangeEndpoint.Start ? _start : _end;

    // Moving one end past the other drags the other end along (a degenerate range), as UIA requires.
    private void Set(TextPatternRangeEndpoint endpoint, int value)
    {
        if (endpoint == TextPatternRangeEndpoint.Start)
        {
            _start = value;
            _end = Math.Max(_end, value);
        }
        else
        {
            _end = value;
            _start = Math.Min(_start, value);
        }
    }

    private static int IndexOfUnit(int[] boundaries, int position) => Math.Max(Array.FindLastIndex(boundaries, x => x <= position), 0);

    // Sorted unit starts, ending with the text length: unit i is [b[i], b[i + 1]).
    private int[] Boundaries(TextUnit unit)
    {
        var b = new List<int> { 0 };
        if (unit is TextUnit.Character or TextUnit.Word or TextUnit.Line or TextUnit.Paragraph or TextUnit.Format)
        {
            for (var i = 1; i < _text.Length; i++)
            {
                if (IsBoundary(unit, i))
                {
                    b.Add(i);
                }
            }
        }

        if (_text.Length > 0)
        {
            b.Add(_text.Length);
        }

        return [.. b];
    }

    private bool IsBoundary(TextUnit unit, int i)
    {
        var prev = _text[i - 1];
        var cur = _text[i];
        if (prev == '\r' && cur == '\n')
        {
            return false;
        }

        return unit switch
        {
            TextUnit.Character => true,
            TextUnit.Word => prev == '\n' || Class(prev) != Class(cur) || Class(cur) == 2,
            _ => prev == '\n',
        };
    }

    private static int Class(char c) => char.IsLetterOrDigit(c) ? 0 : char.IsWhiteSpace(c) ? 1 : 2;
}
