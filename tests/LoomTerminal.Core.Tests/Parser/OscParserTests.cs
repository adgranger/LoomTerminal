// Ported from xtermjs/xterm.js: src/common/parser/OscParser.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Parser;
using static LoomTerminal.Core.Tests.Parser.ParserTestHelpers;

namespace LoomTerminal.Core.Tests.Parser;

public class OscParserTests : IDisposable
{
    private readonly OscParser _parser = new();
    private readonly List<object?[]> _reports = [];

    public OscParserTests()
    {
        _parser.SetHandlerFallback((id, action, data) => _reports.Add([id, action, data]));
    }

    public void Dispose()
    {
        _parser.Dispose();
        GC.SuppressFinalize(this);
    }

    // One recorded call: the items of a `reports` entry.
    private static object?[] R(params ReadOnlySpan<object?> items) => items.ToArray();

    private void Put(string s)
    {
        uint[] data = ToUtf32(s);
        _parser.Put(data, 0, data.Length);
    }

    // xterm.js: 'no report for illegal ids'
    [Fact]
    public void Identifier_parsing_reports_nothing_for_illegal_ids()
    {
        Put("hello world!");
        _parser.End(true);
        Assert.Empty(_reports);
    }

    // xterm.js: 'no payload'
    [Fact]
    public void Identifier_parsing_without_payload()
    {
        _parser.Start();
        Put("12");
        Put("34");
        _parser.End(true);
        Assert.Equal(
            [R(1234, OscFallbackAction.Start, null), R(1234, OscFallbackAction.End, true)],
            _reports);
    }

    // xterm.js: 'with payload'
    [Fact]
    public void Identifier_parsing_with_payload()
    {
        _parser.Start();
        Put("12");
        Put("34");
        Put(";h");
        Put("ello");
        _parser.End(true);
        Assert.Equal(
            [
                R(1234, OscFallbackAction.Start, null),
                R(1234, OscFallbackAction.Put, "h"),
                R(1234, OscFallbackAction.Put, "ello"),
                R(1234, OscFallbackAction.End, true),
            ],
            _reports);
    }

    // xterm.js: 'setOscHandler'
    [Fact]
    public void RegisterHandler_dispatches_to_the_handler()
    {
        _parser.RegisterHandler(1234, new TestOscHandler(1234, _reports, "th"));
        _parser.Start();
        Put("1234;Here comes");
        Put("the mouse!");
        _parser.End(true);
        Assert.Equal(
            [
                // messages from TestHandler
                R("th", 1234, "START"),
                R("th", 1234, "PUT", "Here comes"),
                R("th", 1234, "PUT", "the mouse!"),
                R("th", 1234, "END", true),
            ],
            _reports);
    }

    // xterm.js: 'clearOscHandler'
    [Fact]
    public void ClearHandler_falls_back_to_the_fallback_handler()
    {
        _parser.RegisterHandler(1234, new TestOscHandler(1234, _reports, "th"));
        _parser.ClearHandler(1234);
        _parser.Start();
        Put("1234;Here comes");
        Put("the mouse!");
        _parser.End(true);
        Assert.Equal(
            [
                // messages from fallback handler
                R(1234, OscFallbackAction.Start, null),
                R(1234, OscFallbackAction.Put, "Here comes"),
                R(1234, OscFallbackAction.Put, "the mouse!"),
                R(1234, OscFallbackAction.End, true),
            ],
            _reports);
    }

    // xterm.js: 'addOscHandler'
    [Fact]
    public void RegisterHandler_runs_later_handlers_first_and_stops_at_the_first_true()
    {
        _parser.RegisterHandler(1234, new TestOscHandler(1234, _reports, "th1"));
        _parser.RegisterHandler(1234, new TestOscHandler(1234, _reports, "th2"));
        _parser.Start();
        Put("1234;Here comes");
        Put("the mouse!");
        _parser.End(true);
        Assert.Equal(
            [
                R("th2", 1234, "START"),
                R("th1", 1234, "START"),
                R("th2", 1234, "PUT", "Here comes"),
                R("th1", 1234, "PUT", "Here comes"),
                R("th2", 1234, "PUT", "the mouse!"),
                R("th1", 1234, "PUT", "the mouse!"),
                R("th2", 1234, "END", true),
                R("th1", 1234, "END", false), // false due being already handled by th2!
            ],
            _reports);
    }

    // xterm.js: 'addOscHandler with return false'
    [Fact]
    public void RegisterHandler_keeps_bubbling_when_a_handler_returns_false()
    {
        _parser.RegisterHandler(1234, new TestOscHandler(1234, _reports, "th1"));
        _parser.RegisterHandler(1234, new TestOscHandler(1234, _reports, "th2", true));
        _parser.Start();
        Put("1234;Here comes");
        Put("the mouse!");
        _parser.End(true);
        Assert.Equal(
            [
                R("th2", 1234, "START"),
                R("th1", 1234, "START"),
                R("th2", 1234, "PUT", "Here comes"),
                R("th1", 1234, "PUT", "Here comes"),
                R("th2", 1234, "PUT", "the mouse!"),
                R("th1", 1234, "PUT", "the mouse!"),
                R("th2", 1234, "END", true),
                R("th1", 1234, "END", true), // true since th2 indicated to keep bubbling
            ],
            _reports);
    }

    // xterm.js: 'dispose handlers'
    [Fact]
    public void Disposing_a_registration_removes_the_handler()
    {
        _parser.RegisterHandler(1234, new TestOscHandler(1234, _reports, "th1"));
        IDisposable dispo = _parser.RegisterHandler(1234, new TestOscHandler(1234, _reports, "th2", true));
        dispo.Dispose();
        _parser.Start();
        Put("1234;Here comes");
        Put("the mouse!");
        _parser.End(true);
        Assert.Equal(
            [
                R("th1", 1234, "START"),
                R("th1", 1234, "PUT", "Here comes"),
                R("th1", 1234, "PUT", "the mouse!"),
                R("th1", 1234, "END", true),
            ],
            _reports);
    }

    // xterm.js: 'should be called once on end(true)'
    [Fact]
    public void OscHandler_is_called_once_on_end_true()
    {
        _parser.RegisterHandler(1234, new OscHandler(data => { _reports.Add(R(1234, data)); return true; }));
        _parser.Start();
        Put("1234;Here comes");
        Put(" the mouse!");
        _parser.End(true);
        Assert.Equal([R(1234, "Here comes the mouse!")], _reports);
    }

    // xterm.js: 'should not be called on end(false)'
    [Fact]
    public void OscHandler_is_not_called_on_end_false()
    {
        _parser.RegisterHandler(1234, new OscHandler(data => { _reports.Add(R(1234, data)); return true; }));
        _parser.Start();
        Put("1234;Here comes");
        Put(" the mouse!");
        _parser.End(false);
        Assert.Empty(_reports);
    }

    // xterm.js: 'should be disposable'
    [Fact]
    public void OscHandler_is_disposable()
    {
        _parser.RegisterHandler(1234, new OscHandler(data => { _reports.Add(R("one", data)); return true; }));
        IDisposable dispo = _parser.RegisterHandler(1234, new OscHandler(data => { _reports.Add(R("two", data)); return true; }));
        _parser.Start();
        Put("1234;Here comes");
        Put(" the mouse!");
        _parser.End(true);
        Assert.Equal([R("two", "Here comes the mouse!")], _reports);
        dispo.Dispose();
        _parser.Start();
        Put("1234;some other");
        Put(" data");
        _parser.End(true);
        Assert.Equal([R("two", "Here comes the mouse!"), R("one", "some other data")], _reports);
    }

    // xterm.js: 'should respect return false'
    [Fact]
    public void OscHandler_respects_return_false()
    {
        _parser.RegisterHandler(1234, new OscHandler(data => { _reports.Add(R("one", data)); return true; }));
        _parser.RegisterHandler(1234, new OscHandler(data => { _reports.Add(R("two", data)); return false; }));
        _parser.Start();
        Put("1234;Here comes");
        Put(" the mouse!");
        _parser.End(true);
        Assert.Equal([R("two", "Here comes the mouse!"), R("one", "Here comes the mouse!")], _reports);
    }

    // xterm.js: 'should work up to payload limit'
    [Fact]
    public void OscHandler_works_up_to_the_payload_limit()
    {
        _parser.RegisterHandler(1234, new OscHandler(data => { _reports.Add(R(1234, data)); return true; }));
        _parser.Start();
        Put("1234;");
        uint[] data = ToUtf32(new string('A', 1000));
        for (int i = 0; i < ParserLimits.PayloadLimit; i += 1000)
        {
            _parser.Put(data, 0, data.Length);
        }

        _parser.End(true);
        Assert.Equal([R(1234, new string('A', ParserLimits.PayloadLimit))], _reports);
    }

    // xterm.js: 'should abort for payload limit +1'
    [Fact]
    public void OscHandler_aborts_for_payload_limit_plus_1()
    {
        _parser.RegisterHandler(1234, new OscHandler(data => { _reports.Add(R(1234, data)); return true; }));
        _parser.Start();
        Put("1234;");
        uint[] data = ToUtf32(new string('A', 1000));
        for (int i = 0; i < ParserLimits.PayloadLimit; i += 1000)
        {
            _parser.Put(data, 0, data.Length);
        }

        Put("A");
        _parser.End(true);
        Assert.Empty(_reports);
    }
}
