using System.Text;
using System.Threading.Channels;
using LoomTerminal.Core.Services;

namespace LoomTerminal.Core.Tests;

public sealed class TerminalWriterTests : IDisposable
{
    private readonly TerminalCore _terminal = new(TerminalOptions.Default with { Cols = 80, Rows = 24 });
    private readonly Channel<TerminalWorkItem> _channel = Channel.CreateUnbounded<TerminalWorkItem>();
    private readonly List<int> _batches = [];

    public void Dispose() => _terminal.Dispose();

    private static TerminalWorkItem Output(string text) => TerminalWorkItem.FromOutput(Encoding.UTF8.GetBytes(text));

    private string Line(int row) => _terminal.Buffers.Active.Lines.Get(row)!.TranslateToString(true);

    private void RunToEnd() => Run(TestContext.Current.CancellationToken);

    private void Run(CancellationToken cancellationToken) =>
        new TerminalWriter(_terminal).Run(_channel.Reader, _batches.Add, cancellationToken);

    [Fact]
    public void Items_queued_before_Run_form_one_batch_with_the_total_length()
    {
        _channel.Writer.TryWrite(Output("ab"));
        _channel.Writer.TryWrite(Output("cd"));
        _channel.Writer.TryWrite(Output("ef"));
        _channel.Writer.Complete();
        RunToEnd();
        Assert.Equal<int>([6], _batches);
        Assert.Equal("abcdef", Line(0));
    }

    [Fact]
    public void An_action_runs_between_the_outputs_that_surround_it()
    {
        List<string> seen = [];
        _channel.Writer.TryWrite(Output("one"));
        _channel.Writer.TryWrite(TerminalWorkItem.FromAction(core => seen.Add(core.Buffers.Active.Lines.Get(0)!.TranslateToString(true))));
        _channel.Writer.TryWrite(Output("two"));
        _channel.Writer.Complete();
        RunToEnd();
        Assert.Equal<string>(["one"], seen);
        Assert.Equal("onetwo", Line(0));
    }

    [Fact]
    public void A_UTF8_sequence_split_across_two_items_prints_one_character()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("é");
        _channel.Writer.TryWrite(TerminalWorkItem.FromOutput(bytes[..1]));
        _channel.Writer.TryWrite(TerminalWorkItem.FromOutput(bytes[1..]));
        _channel.Writer.Complete();
        RunToEnd();
        Assert.Equal("é", Line(0));
        Assert.Equal(1, _terminal.Buffers.Active.X);
    }

    [Fact]
    public void Completing_the_channel_ends_Run_without_a_batch()
    {
        _channel.Writer.Complete();
        RunToEnd();
        Assert.Empty(_batches);
    }

    [Fact]
    public void Cancelling_ends_Run_with_an_OperationCanceledException()
    {
        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() => Run(cts.Token));
    }

    [Fact]
    public void Two_megabytes_queued_at_once_are_split_in_several_batches()
    {
        byte[] chunk = new byte[64 * 1024];
        Array.Fill(chunk, (byte)'a');
        for (int i = 0; i < 32; i++)
        {
            _channel.Writer.TryWrite(TerminalWorkItem.FromOutput(chunk));
        }

        _channel.Writer.Complete();
        RunToEnd();
        Assert.True(_batches.Count >= 4);
        Assert.Equal(32 * 64 * 1024, _batches.Sum());
    }

    [Fact]
    public void An_exception_of_an_action_propagates_out_of_Run()
    {
        _channel.Writer.TryWrite(TerminalWorkItem.FromAction(_ => throw new InvalidOperationException("boom")));
        _channel.Writer.Complete();
        Assert.Equal("boom", Assert.Throws<InvalidOperationException>(RunToEnd).Message);
    }

    [Fact]
    public async Task Run_waits_for_work_that_arrives_later()
    {
        Task run = Task.Run(RunToEnd, TestContext.Current.CancellationToken);
        _channel.Writer.TryWrite(Output("late"));
        _channel.Writer.Complete();
        await run;
        Assert.Equal("late", Line(0));
    }
}
