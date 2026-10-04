using Forma.Builder;

namespace Forma.Tests;

public class CoalescedRefreshTests
{
    private sealed class UiContext : SynchronizationContext
    {
        private readonly Queue<Action> _posts = new();
        public override void Post(SendOrPostCallback callback, object? state) => _posts.Enqueue(() => callback(state));
        public void Drain() { while (_posts.TryDequeue(out var action)) action(); }
    }

    [Fact]
    public void SameTurnRequestsProduceOneSendWithLatestStatus()
    {
        var previous = SynchronizationContext.Current; var context = new UiContext(); SynchronizationContext.SetSynchronizationContext(context);
        try {
            var statuses = new List<string>();
            var refresh = new CoalescedRefresh(status => { statuses.Add(status); return Task.CompletedTask; });
            var first = refresh.Request("Property changed");
            for (int i = 0; i < 100; i++) Assert.Same(first, refresh.Request($"Edit {i}"));
            Assert.Empty(statuses); context.Drain(); Assert.True(first.IsCompletedSuccessfully); Assert.Equal(new[] { "Edit 99" }, statuses);
            var next = refresh.Request("Next turn"); context.Drain(); Assert.True(next.IsCompletedSuccessfully); Assert.Equal(2, statuses.Count);
        } finally { SynchronizationContext.SetSynchronizationContext(previous); }
    }

    [Fact]
    public void RequestsDuringSendAndErrorsDoNotLoseFutureRefreshes()
    {
        var previous = SynchronizationContext.Current; var context = new UiContext(); SynchronizationContext.SetSynchronizationContext(context);
        try {
            var statuses = new List<string>(); CoalescedRefresh? refresh = null;
            refresh = new CoalescedRefresh(status => {
                statuses.Add(status);
                if (status == "First") refresh!.Request("Follow-up");
                return status == "Failure" ? Task.FromException(new InvalidOperationException("Test failure")) : Task.CompletedTask;
            });
            refresh.Request("First"); context.Drain(); Assert.Equal(new[] { "First", "Follow-up" }, statuses);
            var failed = refresh.Request("Failure"); context.Drain(); Assert.True(failed.IsFaulted);
            var recovery = refresh.Request("Recovery"); context.Drain(); Assert.True(recovery.IsCompletedSuccessfully); Assert.Equal("Recovery", statuses.Last());
        } finally { SynchronizationContext.SetSynchronizationContext(previous); }
    }
}
