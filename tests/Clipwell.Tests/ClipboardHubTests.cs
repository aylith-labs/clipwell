using Clipwell.Daemon;
using Xunit;

namespace Clipwell.Tests;

public sealed class ClipboardHubTests
{
    [Fact]
    public void SubscriberCount_StartsAtZero() => Assert.Equal(0, new ClipboardHub().SubscriberCount);

    [Fact]
    public void Subscribe_TracksEachSubscriber()
    {
        var hub = new ClipboardHub();

        hub.Subscribe();
        hub.Subscribe();

        Assert.Equal(2, hub.SubscriberCount);
    }

    [Fact]
    public void Subscribe_HandsOutDistinctIds()
    {
        var hub = new ClipboardHub();

        var (first, _) = hub.Subscribe();
        var (second, _) = hub.Subscribe();

        Assert.NotEqual(first, second);
    }

    [Fact]
    public async Task Broadcast_ReachesEverySubscriber()
    {
        var hub = new ClipboardHub();
        var (_, first) = hub.Subscribe();
        var (_, second) = hub.Subscribe();

        hub.Broadcast("payload");

        Assert.Equal("payload", await first.ReadAsync(TestContext.Current.CancellationToken));
        Assert.Equal("payload", await second.ReadAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Broadcast_WithNoSubscribersIsHarmless() => new ClipboardHub().Broadcast("payload");

    [Fact]
    public async Task Broadcast_PreservesOrder()
    {
        var hub = new ClipboardHub();
        var (_, reader) = hub.Subscribe();

        hub.Broadcast("first");
        hub.Broadcast("second");

        Assert.Equal("first", await reader.ReadAsync(TestContext.Current.CancellationToken));
        Assert.Equal("second", await reader.ReadAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Unsubscribe_RemovesTheSubscriber()
    {
        var hub = new ClipboardHub();
        var (id, _) = hub.Subscribe();

        hub.Unsubscribe(id);

        Assert.Equal(0, hub.SubscriberCount);
    }

    [Fact]
    public async Task Unsubscribe_CompletesTheChannelSoTheReaderLoopExits()
    {
        var hub = new ClipboardHub();
        var (id, reader) = hub.Subscribe();

        hub.Unsubscribe(id);

        var received = new List<string>();
        await foreach (var payload in reader.ReadAllAsync(TestContext.Current.CancellationToken))
            received.Add(payload);
        Assert.Empty(received);
    }

    [Fact]
    public void Unsubscribe_AnUnknownIdIsHarmless() => new ClipboardHub().Unsubscribe(Guid.NewGuid());

    [Fact]
    public void Unsubscribe_TwiceIsHarmless()
    {
        var hub = new ClipboardHub();
        var (id, _) = hub.Subscribe();

        hub.Unsubscribe(id);
        hub.Unsubscribe(id);

        Assert.Equal(0, hub.SubscriberCount);
    }

    [Fact]
    public void Broadcast_AfterUnsubscribeDoesNotReachTheDroppedSubscriber()
    {
        var hub = new ClipboardHub();
        var (id, reader) = hub.Subscribe();
        hub.Unsubscribe(id);

        hub.Broadcast("payload");

        Assert.False(reader.TryRead(out _));
    }

    [Fact]
    public async Task Broadcast_ASlowSubscriberLosesOldEventsInsteadOfBlockingTheWatcher()
    {
        // The channel is bounded with DropOldest: a client that stops reading must
        // never back-pressure the capture pipeline.
        var hub = new ClipboardHub();
        var (_, reader) = hub.Subscribe();

        for (var index = 0; index < 500; index++) hub.Broadcast($"event-{index}");

        // The newest event is still there; the oldest were dropped.
        var drained = new List<string>();
        while (reader.TryRead(out var payload)) drained.Add(payload);
        Assert.Equal("event-499", drained[^1]);
        Assert.DoesNotContain("event-0", drained);
        await Task.CompletedTask;
    }

    [Fact]
    public void Broadcast_ASlowSubscriberDoesNotStarveAHealthyOne()
    {
        var hub = new ClipboardHub();
        var (_, stalled) = hub.Subscribe();
        var (_, healthy) = hub.Subscribe();
        for (var index = 0; index < 200; index++) hub.Broadcast($"event-{index}");
        while (healthy.TryRead(out _)) { }

        hub.Broadcast("latest");

        Assert.True(healthy.TryRead(out var payload));
        Assert.Equal("latest", payload);
        Assert.True(stalled.TryRead(out _));
    }

    [Fact]
    public void ConcurrentSubscribeAndBroadcastStayConsistent()
    {
        var hub = new ClipboardHub();

        Parallel.For(0, 100, _ =>
        {
            var (id, _) = hub.Subscribe();
            hub.Broadcast("payload");
            hub.Unsubscribe(id);
        });

        Assert.Equal(0, hub.SubscriberCount);
    }
}
