using LoomTerminal.Core.Common;

namespace LoomTerminal.Core.Tests.Common;

public class DisposableTests
{
    private sealed class Owner(List<string> log) : Disposable
    {
        public void Add(string name) => Register(Disposables.Create(() => log.Add(name)));

        public void AddObject(IDisposable disposable) => Register(disposable);

        protected override void DisposeCore() => log.Add("core");
    }

    [Fact]
    public void Dispose_disposes_registered_objects_once_in_reverse_order()
    {
        List<string> log = [];
        Owner owner = new(log);
        owner.Add("a");
        owner.Add("b");

        owner.Dispose();
        owner.Dispose();

        Assert.Equal(["b", "a", "core"], log);
        Assert.True(owner.IsDisposed);
    }

    [Fact]
    public void Register_after_dispose_disposes_the_argument_immediately()
    {
        List<string> log = [];
        Owner owner = new(log);
        owner.Dispose();
        log.Clear();

        owner.Add("late");

        Assert.Equal(["late"], log);
    }

    [Fact]
    public void Disposables_Create_runs_its_action_once()
    {
        int count = 0;
        IDisposable disposable = Disposables.Create(() => count++);

        disposable.Dispose();
        disposable.Dispose();

        Assert.Equal(1, count);
    }

    [Fact]
    public void MutableDisposable_setter_disposes_the_previous_value_and_Clear_disposes_and_nulls()
    {
        int first = 0;
        int second = 0;
        MutableDisposable<IDisposable> holder = new();

        holder.Value = Disposables.Create(() => first++);
        IDisposable secondValue = Disposables.Create(() => second++);
        holder.Value = secondValue;
        Assert.Equal(1, first);
        Assert.Equal(0, second);

        holder.Clear();

        Assert.Equal(1, second);
        Assert.Null(holder.Value);
    }

    [Fact]
    public void DisposableStore_Clear_disposes_and_empties()
    {
        int count = 0;
        DisposableStore store = new();
        store.Add(Disposables.Create(() => count++));

        store.Clear();
        store.Clear();
        store.Dispose();

        Assert.Equal(1, count);
    }
}
