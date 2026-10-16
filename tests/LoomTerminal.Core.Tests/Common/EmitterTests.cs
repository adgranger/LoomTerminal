using LoomTerminal.Core.Common;

namespace LoomTerminal.Core.Tests.Common;

public class EmitterTests
{
    [Fact]
    public void Fire_calls_listeners_in_subscription_order()
    {
        Emitter<int> emitter = new();
        List<string> log = [];
        emitter.Event.Subscribe(v => log.Add("a" + v));
        emitter.Event.Subscribe(v => log.Add("b" + v));

        emitter.Fire(1);

        Assert.Equal(["a1", "b1"], log);
    }

    [Fact]
    public void Disposing_the_subscription_stops_calls()
    {
        Emitter<int> emitter = new();
        int count = 0;
        IDisposable subscription = emitter.Event.Subscribe(_ => count++);

        emitter.Fire(1);
        subscription.Dispose();
        emitter.Fire(2);

        Assert.Equal(1, count);
    }

    [Fact]
    public void A_listener_removed_during_Fire_is_not_called_afterwards()
    {
        Emitter<int> emitter = new();
        int secondCalls = 0;
        IDisposable? second = null;
        emitter.Event.Subscribe(_ => second!.Dispose());
        second = emitter.Event.Subscribe(_ => secondCalls++);

        emitter.Fire(1);

        Assert.Equal(0, secondCalls);
    }

    [Fact]
    public void A_listener_added_during_Fire_is_called_from_the_next_Fire()
    {
        Emitter<int> emitter = new();
        int lateCalls = 0;
        bool added = false;
        emitter.Event.Subscribe(_ =>
        {
            if (!added)
            {
                added = true;
                emitter.Event.Subscribe(_ => lateCalls++);
            }
        });

        emitter.Fire(1);
        Assert.Equal(0, lateCalls);

        emitter.Fire(2);
        Assert.Equal(1, lateCalls);
    }

    [Fact]
    public void Fire_after_Dispose_does_nothing()
    {
        Emitter<int> emitter = new();
        int count = 0;
        emitter.Event.Subscribe(_ => count++);

        emitter.Dispose();
        emitter.Fire(1);

        Assert.Equal(0, count);
    }
}
