// Ported from xtermjs/xterm.js: src/common/InputHandler.test.ts
// Upstream commit: f447274f430fd22513f6adbf9862d19524471c04  —  Copyright (c) The xterm.js authors. MIT License.

using LoomTerminal.Core.Buffer;
using LoomTerminal.Core.InputHandling;
using LoomTerminal.Core.Parser;
using LoomTerminal.Core.Services;
using LoomTerminal.Core.Tests.TestUtils;

namespace LoomTerminal.Core.Tests.InputHandling;

/// <summary>An input handler that exposes its internals to the tests.</summary>
/// <remarks>Threading: not thread-safe; used from a single test thread.</remarks>
internal sealed class TestInputHandler(
    IBufferService bufferService,
    ICharsetService charsetService,
    ICoreService coreService,
    ILogService logService,
    IOptionsService optionsService,
    IOscLinkService oscLinkService,
    ICoreMouseService coreMouseService,
    IUnicodeService unicodeService,
    IEscapeSequenceParser? parser = null)
    : InputHandler(bufferService, charsetService, coreService, logService, optionsService, oscLinkService, coreMouseService, unicodeService, parser)
{
    public AttributeData CurAttrData => GetAttrData();

    public List<string> WindowTitleStack => _windowTitleStack;

    public List<string> IconNameStack => _iconNameStack;
}

/// <summary>The setup shared by the InputHandler test classes (the <c>beforeEach</c> of InputHandler.test.ts).</summary>
/// <remarks>Threading: not thread-safe; used from a single test thread.</remarks>
public abstract class InputHandlerTestBase : IDisposable
{
    private readonly MockOptionsService _optionsServiceOwner;
    private readonly BufferService _bufferServiceOwner;
    private readonly CoreService _coreServiceOwner;
    private readonly OscLinkService _oscLinkServiceOwner;
    private readonly MockCoreMouseService _coreMouseServiceOwner = new();

    private protected InputHandlerTestBase(ICharsetService? charsetService = null)
    {
        _optionsServiceOwner = new MockOptionsService();
        _bufferServiceOwner = new BufferService(_optionsServiceOwner);
        _bufferServiceOwner.Resize(80, 30);
        LogService = new MockLogService();
        _coreServiceOwner = new CoreService(_bufferServiceOwner, new MockLogService(), _optionsServiceOwner);
        _oscLinkServiceOwner = new OscLinkService(_bufferServiceOwner);
        InputHandler = new TestInputHandler(
            _bufferServiceOwner,
            charsetService ?? new MockCharsetService(),
            _coreServiceOwner,
            LogService,
            _optionsServiceOwner,
            _oscLinkServiceOwner,
            _coreMouseServiceOwner,
            new MockUnicodeService());
    }

    private protected MockOptionsService OptionsService => _optionsServiceOwner;

    private protected BufferService BufferService => _bufferServiceOwner;

    private protected CoreService CoreService => _coreServiceOwner;

    private protected OscLinkService OscLinkService => _oscLinkServiceOwner;

    private protected MockLogService LogService { get; }

    private protected TestInputHandler InputHandler { get; }

    /// <summary>The cursor as (x, y).</summary>
    private protected int[] GetCursor() => [BufferService.Buffer.X, BufferService.Buffer.Y];

    /// <summary>The first <paramref name="limit"/> lines (all rows by default) with trailing whitespace trimmed.</summary>
    private protected List<string> GetLines(int? limit = null)
    {
        List<string> result = [];
        for (int i = 0; i < (limit ?? BufferService.Rows); ++i)
        {
            IBufferLine? line = BufferService.Buffer.Lines.Get(i);
            if (line is not null)
            {
                result.Add(line.TranslateToString(true));
            }
        }

        return result;
    }

    public void Dispose()
    {
        InputHandler.Dispose();
        _coreServiceOwner.Dispose();
        _bufferServiceOwner.Dispose();
        _coreMouseServiceOwner.Dispose();
        _optionsServiceOwner.Dispose();
        GC.SuppressFinalize(this);
    }
}
