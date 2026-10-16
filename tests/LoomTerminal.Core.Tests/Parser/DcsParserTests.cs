// Ported from xtermjs/xterm.js: src/common/parser/DcsParser.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Parser;
using static LoomTerminal.Core.Tests.Parser.ParserTestHelpers;

namespace LoomTerminal.Core.Tests.Parser;

public class DcsParserTests : IDisposable
{
    private static readonly int PlusP = Identifier(new FunctionIdentifier(Intermediates: "+", Final: "p"));

    private readonly DcsParser _parser = new();
    private readonly List<object?[]> _reports = [];

    public DcsParserTests()
    {
        _parser.SetHandlerFallback((id, action, data) =>
        {
            if (action == DcsFallbackAction.Hook)
            {
                data = ((IParams)data!).ToArray();
            }

            _reports.Add([id, action, data]);
        });
    }

    public void Dispose()
    {
        _parser.Dispose();
        GC.SuppressFinalize(this);
    }

    // One recorded call: the items of a `reports` entry.
    private static object?[] R(params ReadOnlySpan<object?> items) => items.ToArray();

    private static object[] A(params ReadOnlySpan<object> items) => items.ToArray();

    private static Params Abc() => Params.FromArray(A(1, 2, 3));

    private void Put(string s)
    {
        uint[] data = ToUtf32(s);
        _parser.Put(data, 0, data.Length);
    }

    // xterm.js: 'setDcsHandler'
    [Fact]
    public void RegisterHandler_dispatches_to_the_handler()
    {
        _parser.RegisterHandler(PlusP, new TestDcsHandler(_reports, "th"));
        _parser.Hook(PlusP, Abc());
        Put("Here comes");
        Put("the mouse!");
        _parser.Unhook(true);
        Assert.Equal(
            [
                // messages from TestHandler
                R("th", "HOOK", A(1, 2, 3)),
                R("th", "PUT", "Here comes"),
                R("th", "PUT", "the mouse!"),
                R("th", "UNHOOK", true),
            ],
            _reports);
    }

    // xterm.js: 'clearDcsHandler'
    [Fact]
    public void ClearHandler_falls_back_to_the_fallback_handler()
    {
        _parser.RegisterHandler(PlusP, new TestDcsHandler(_reports, "th"));
        _parser.ClearHandler(PlusP);
        _parser.Hook(PlusP, Abc());
        Put("Here comes");
        Put("the mouse!");
        _parser.Unhook(true);
        Assert.Equal(
            [
                // messages from fallback handler
                R(PlusP, DcsFallbackAction.Hook, A(1, 2, 3)),
                R(PlusP, DcsFallbackAction.Put, "Here comes"),
                R(PlusP, DcsFallbackAction.Put, "the mouse!"),
                R(PlusP, DcsFallbackAction.Unhook, true),
            ],
            _reports);
    }

    // xterm.js: 'addDcsHandler'
    [Fact]
    public void RegisterHandler_runs_later_handlers_first_and_stops_at_the_first_true()
    {
        _parser.RegisterHandler(PlusP, new TestDcsHandler(_reports, "th1"));
        _parser.RegisterHandler(PlusP, new TestDcsHandler(_reports, "th2"));
        _parser.Hook(PlusP, Abc());
        Put("Here comes");
        Put("the mouse!");
        _parser.Unhook(true);
        Assert.Equal(
            [
                R("th2", "HOOK", A(1, 2, 3)),
                R("th1", "HOOK", A(1, 2, 3)),
                R("th2", "PUT", "Here comes"),
                R("th1", "PUT", "Here comes"),
                R("th2", "PUT", "the mouse!"),
                R("th1", "PUT", "the mouse!"),
                R("th2", "UNHOOK", true),
                R("th1", "UNHOOK", false), // false due being already handled by th2!
            ],
            _reports);
    }

    // xterm.js: 'addDcsHandler with return false'
    [Fact]
    public void RegisterHandler_keeps_bubbling_when_a_handler_returns_false()
    {
        _parser.RegisterHandler(PlusP, new TestDcsHandler(_reports, "th1"));
        _parser.RegisterHandler(PlusP, new TestDcsHandler(_reports, "th2", true));
        _parser.Hook(PlusP, Abc());
        Put("Here comes");
        Put("the mouse!");
        _parser.Unhook(true);
        Assert.Equal(
            [
                R("th2", "HOOK", A(1, 2, 3)),
                R("th1", "HOOK", A(1, 2, 3)),
                R("th2", "PUT", "Here comes"),
                R("th1", "PUT", "Here comes"),
                R("th2", "PUT", "the mouse!"),
                R("th1", "PUT", "the mouse!"),
                R("th2", "UNHOOK", true),
                R("th1", "UNHOOK", true), // true since th2 indicated to keep bubbling
            ],
            _reports);
    }

    // xterm.js: 'dispose handlers'
    [Fact]
    public void Disposing_a_registration_removes_the_handler()
    {
        _parser.RegisterHandler(PlusP, new TestDcsHandler(_reports, "th1"));
        IDisposable dispo = _parser.RegisterHandler(PlusP, new TestDcsHandler(_reports, "th2", true));
        dispo.Dispose();
        _parser.Hook(PlusP, Abc());
        Put("Here comes");
        Put("the mouse!");
        _parser.Unhook(true);
        Assert.Equal(
            [
                R("th1", "HOOK", A(1, 2, 3)),
                R("th1", "PUT", "Here comes"),
                R("th1", "PUT", "the mouse!"),
                R("th1", "UNHOOK", true),
            ],
            _reports);
    }

    // xterm.js: 'should be called once on end(true)'
    [Fact]
    public void DcsHandler_is_called_once_on_unhook_true()
    {
        _parser.RegisterHandler(PlusP, new DcsHandler((data, p) => { _reports.Add(R(p.ToArray(), data)); return true; }));
        _parser.Hook(PlusP, Abc());
        Put("Here comes");
        Put(" the mouse!");
        _parser.Unhook(true);
        Assert.Equal([R(A(1, 2, 3), "Here comes the mouse!")], _reports);
    }

    // xterm.js: 'should not be called on end(false)'
    [Fact]
    public void DcsHandler_is_not_called_on_unhook_false()
    {
        _parser.RegisterHandler(PlusP, new DcsHandler((data, p) => { _reports.Add(R(p.ToArray(), data)); return true; }));
        _parser.Hook(PlusP, Abc());
        Put("Here comes");
        Put(" the mouse!");
        _parser.Unhook(false);
        Assert.Empty(_reports);
    }

    // xterm.js: 'should be disposable'
    [Fact]
    public void DcsHandler_is_disposable()
    {
        _parser.RegisterHandler(PlusP, new DcsHandler((data, p) => { _reports.Add(R("one", p.ToArray(), data)); return true; }));
        IDisposable dispo = _parser.RegisterHandler(PlusP, new DcsHandler((data, p) => { _reports.Add(R("two", p.ToArray(), data)); return true; }));
        _parser.Hook(PlusP, Abc());
        Put("Here comes");
        Put(" the mouse!");
        _parser.Unhook(true);
        Assert.Equal([R("two", A(1, 2, 3), "Here comes the mouse!")], _reports);
        dispo.Dispose();
        _parser.Hook(PlusP, Abc());
        Put("some other");
        Put(" data");
        _parser.Unhook(true);
        Assert.Equal([R("two", A(1, 2, 3), "Here comes the mouse!"), R("one", A(1, 2, 3), "some other data")], _reports);
    }

    // xterm.js: 'should respect return false'
    [Fact]
    public void DcsHandler_respects_return_false()
    {
        _parser.RegisterHandler(PlusP, new DcsHandler((data, p) => { _reports.Add(R("one", p.ToArray(), data)); return true; }));
        _parser.RegisterHandler(PlusP, new DcsHandler((data, p) => { _reports.Add(R("two", p.ToArray(), data)); return false; }));
        _parser.Hook(PlusP, Abc());
        Put("Here comes");
        Put(" the mouse!");
        _parser.Unhook(true);
        Assert.Equal([R("two", A(1, 2, 3), "Here comes the mouse!"), R("one", A(1, 2, 3), "Here comes the mouse!")], _reports);
    }

    // xterm.js: 'should work up to payload limit'
    [Fact]
    public void DcsHandler_works_up_to_the_payload_limit()
    {
        _parser.RegisterHandler(PlusP, new DcsHandler((data, p) => { _reports.Add(R(p.ToArray(), data)); return true; }));
        _parser.Hook(PlusP, Abc());
        uint[] data = ToUtf32(new string('A', 1000));
        for (int i = 0; i < ParserLimits.PayloadLimit; i += 1000)
        {
            _parser.Put(data, 0, data.Length);
        }

        _parser.Unhook(true);
        Assert.Equal([R(A(1, 2, 3), new string('A', ParserLimits.PayloadLimit))], _reports);
    }

    // xterm.js: 'should abort for payload limit +1'
    [Fact]
    public void DcsHandler_aborts_for_payload_limit_plus_1()
    {
        _parser.RegisterHandler(PlusP, new DcsHandler((data, p) => { _reports.Add(R(p.ToArray(), data)); return true; }));
        _parser.Hook(PlusP, Abc());
        uint[] data = ToUtf32(new string('A', 1000));
        for (int i = 0; i < ParserLimits.PayloadLimit; i += 1000)
        {
            _parser.Put(data, 0, data.Length);
        }

        Put("A");
        _parser.Unhook(true);
        Assert.Empty(_reports);
    }
}
