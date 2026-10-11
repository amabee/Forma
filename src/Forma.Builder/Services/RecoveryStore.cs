using System.Text.Json;

namespace Forma.Builder;

/// <summary>An autosaved design left behind by a Builder session that did not close normally.</summary>
public sealed record RecoveryEntry(string Id, string DocumentPath, string? ProjectPath, DateTime SavedAtUtc);

/// <summary>
/// Autosave files for crash recovery. Each running Builder owns one session, marked by a
/// lock file it keeps open; sessions whose lock is free were left behind by a crash or
/// power loss and can be offered for restore.
/// </summary>
public sealed class RecoveryStore(string folder)
{
    public static RecoveryStore Default { get; } = new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Forma", "Recovery"));

    public string Folder { get; } = folder;

    public RecoverySession Begin()
    {
        Directory.CreateDirectory(Folder);
        var id = $"{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
        var lockFile = new FileStream(Path.Combine(Folder, id + ".lock"), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.DeleteOnClose);
        return new RecoverySession(this, id, lockFile);
    }

    /// <summary>Autosaves from sessions that are no longer running, newest first.</summary>
    public IReadOnlyList<RecoveryEntry> FindAbandoned()
    {
        if (!Directory.Exists(Folder)) return [];
        var entries = new List<RecoveryEntry>();
        foreach (var metadata in Directory.EnumerateFiles(Folder, "*.json"))
        {
            var id = Path.GetFileNameWithoutExtension(metadata);
            if (IsRunning(id)) continue;
            try
            {
                var info = JsonSerializer.Deserialize<Metadata>(File.ReadAllText(metadata));
                var document = DocumentPath(id);
                if (info is not null && File.Exists(document))
                    entries.Add(new(id, document, info.ProjectPath, info.SavedAtUtc));
            }
            catch (Exception error) when (error is IOException or JsonException or UnauthorizedAccessException) { }
        }
        return entries.OrderByDescending(entry => entry.SavedAtUtc).ToList();
    }

    public void Discard(string id)
    {
        foreach (var path in new[] { DocumentPath(id), MetadataPath(id) })
            try { File.Delete(path); } catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
    }

    internal string DocumentPath(string id) => Path.Combine(Folder, id + ".forma");
    internal string MetadataPath(string id) => Path.Combine(Folder, id + ".json");

    private bool IsRunning(string id)
    {
        var lockPath = Path.Combine(Folder, id + ".lock");
        if (!File.Exists(lockPath)) return false;
        try
        {
            // An open lock means the owning Builder is still running.
            using var probe = new FileStream(lockPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException) { return true; }
        catch (UnauthorizedAccessException) { return true; }
        try { File.Delete(lockPath); } catch (IOException) { }
        return false;
    }

    internal sealed record Metadata(string? ProjectPath, DateTime SavedAtUtc);
}

/// <summary>The autosave slot of one running Builder.</summary>
public sealed class RecoverySession : IDisposable
{
    private readonly RecoveryStore _store;
    private readonly FileStream _lock;

    internal RecoverySession(RecoveryStore store, string id, FileStream lockFile)
    {
        _store = store;
        Id = id;
        _lock = lockFile;
    }

    public string Id { get; }

    public void Save(ProjectDocument document, string? projectPath)
    {
        ProjectFile.Write(_store.DocumentPath(Id), document);
        File.WriteAllText(_store.MetadataPath(Id), JsonSerializer.Serialize(new RecoveryStore.Metadata(projectPath, DateTime.UtcNow)));
    }

    /// <summary>Removes the autosave, e.g. after a successful save or a deliberate discard.</summary>
    public void Clear() => _store.Discard(Id);

    public void Dispose()
    {
        Clear();
        _lock.Dispose();
    }
}
