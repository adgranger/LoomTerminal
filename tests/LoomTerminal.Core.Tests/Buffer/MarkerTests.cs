using LoomTerminal.Core.Buffer;
using LoomTerminal.Core.Common;

namespace LoomTerminal.Core.Tests.Buffer;

// New tests: the xterm.js sources of Marker and of the reflow larger helpers have no unit tests at this commit
// (the reflow larger behavior is covered by the Buffer tests of P1-32).
public class MarkerTests
{
    [Fact]
    public void Markers_get_increasing_ids()
    {
        Marker first = new(0);
        Marker second = new(0);
        Assert.True(second.Id > first.Id);
    }

    [Fact]
    public void Dispose_sets_the_line_to_minus_one_and_raises_OnDispose_once()
    {
        Marker marker = new(5);
        int raised = 0;
        marker.OnDispose.Subscribe(_ => raised++);

        marker.Dispose();
        marker.Dispose();

        Assert.True(marker.IsDisposed);
        Assert.Equal(-1, marker.Line);
        Assert.Equal(1, raised);
    }

    [Fact]
    public void Dispose_disposes_the_registered_objects()
    {
        Marker marker = new(5);
        bool disposed = false;
        marker.Register(Disposables.Create(() => disposed = true));
        marker.Dispose();
        Assert.True(disposed);
    }

    [Fact]
    public void ReflowLargerCreateNewLayout_skips_removed_lines_and_notifies_the_deletion()
    {
        CircularList<IBufferLine> lines = new(10);
        for (int i = 0; i < 6; i++)
        {
            lines.Push(new BufferLine(2));
        }

        List<DeleteEvent> deletions = [];
        lines.OnDelete.Subscribe(deletions.Add);
        lines.OnDeleteEmitter.Fire(new DeleteEvent(0, 0));
        deletions.Clear();

        NewLayoutResult result = BufferReflow.ReflowLargerCreateNewLayout(lines, [2, 2, 5, 1]);

        Assert.Equal([0, 1, 4], result.Layout);
        Assert.Equal(3, result.CountRemoved);
        Assert.Equal([new DeleteEvent(2, 2), new DeleteEvent(3, 1)], deletions);

        BufferReflow.ReflowLargerApplyNewLayout(lines, result.Layout);
        Assert.Equal(3, lines.Length);
    }

    [Fact]
    public void ReflowLargerCreateNewLayout_keeps_every_line_when_nothing_is_removed()
    {
        CircularList<IBufferLine> lines = new(10);
        lines.Push(new BufferLine(2));
        lines.Push(new BufferLine(2));
        NewLayoutResult result = BufferReflow.ReflowLargerCreateNewLayout(lines, []);
        Assert.Equal([0, 1], result.Layout);
        Assert.Equal(0, result.CountRemoved);
    }
}
