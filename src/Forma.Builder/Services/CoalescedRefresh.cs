namespace Forma.Builder;

/// <summary>Merge requests on the owning UI context before building an expensive state message.</summary>
public sealed class CoalescedRefresh(Func<string, Task> send)
{
    private Task? _pending;
    private string _status = "Ready";
    public Task Request(string status)
    {
        _status = status;
        return _pending ??= FlushAsync();
    }
    private async Task FlushAsync()
    {
        await Task.Yield();
        // A request arriving during send must schedule a new refresh, not be lost.
        var status = _status;
        _pending = null;
        await send(status);
    }
}
