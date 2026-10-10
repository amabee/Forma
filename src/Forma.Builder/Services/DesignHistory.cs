namespace Forma.Builder;

public sealed record DesignSnapshot(string Json, string? SelectedId, string? ActiveFormId = null);

public sealed record DesignEdit(
    DesignSnapshot Before,
    DesignSnapshot After,
    string? Group,
    DateTimeOffset At
);

/// <summary>Bounded design history; consecutive typing in the same field is one edit.</summary>
public sealed class DesignHistory
{
    private readonly List<DesignEdit> _undo = [],
        _redo = [];
    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    public bool Record(
        DesignSnapshot before,
        DesignSnapshot after,
        string? group = null,
        DateTimeOffset? time = null
    )
    {
        if (before.Json == after.Json)
            return false;
        var at = time ?? DateTimeOffset.UtcNow;
        if (
            group is not null
            && _redo.Count == 0
            && _undo.LastOrDefault() is { } previous
            && previous.Group == group
            && previous.After.Json == before.Json
            && at - previous.At < TimeSpan.FromMilliseconds(800)
        )
        {
            _undo[^1] = previous with { After = after, At = at };
            if (previous.Before.Json == after.Json)
                _undo.RemoveAt(_undo.Count - 1);
        }
        else
            _undo.Add(new(before, after, group, at));
        _redo.Clear();
        while (
            _undo.Count > 100
            || (
                _undo.Count > 1
                && _undo.Sum(e => (long)e.Before.Json.Length + e.After.Json.Length)
                    > 32 * 1024 * 1024
            )
        )
            _undo.RemoveAt(0);
        return true;
    }

    public DesignSnapshot? Undo()
    {
        if (!CanUndo)
            return null;
        var edit = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);
        _redo.Add(edit);
        return edit.Before;
    }

    public DesignSnapshot? Redo()
    {
        if (!CanRedo)
            return null;
        var edit = _redo[^1];
        _redo.RemoveAt(_redo.Count - 1);
        _undo.Add(edit);
        return edit.After;
    }

    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
    }
}
