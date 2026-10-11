using Forma.Builder;

namespace Forma.Tests;

public sealed class RecoveryStoreTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"forma-recovery-{Guid.NewGuid():N}");

    public void Dispose()
    {
        try { Directory.Delete(_folder, true); } catch (IOException) { }
    }

    private static ProjectDocument Design(string title)
    {
        var model = new BuilderViewModel(); model.CreateNew();
        model.Form.Title = title;
        return model.CaptureProject();
    }

    [Fact]
    public void RunningSessionsAreNotOfferedAndCleanExitsLeaveNothing()
    {
        var store = new RecoveryStore(_folder);
        using (var session = store.Begin())
        {
            session.Save(Design("Live"), @"C:\Designs\Live.forma");
            Assert.Empty(store.FindAbandoned());
        }
        Assert.Empty(store.FindAbandoned());
        Assert.Empty(Directory.EnumerateFiles(_folder));
    }

    [Fact]
    public void AutosavesFromSessionsThatDidNotCloseAreRestorable()
    {
        // A crashed session leaves its autosave and a lock file nobody holds open.
        var store = new RecoveryStore(_folder);
        Directory.CreateDirectory(_folder);
        ProjectFile.Write(Path.Combine(_folder, "crashed.forma"), Design("Unsaved work"));
        File.WriteAllText(Path.Combine(_folder, "crashed.json"), """{"ProjectPath":"C:\\Designs\\App.forma","SavedAtUtc":"2026-10-11T08:00:00Z"}""");
        File.Create(Path.Combine(_folder, "crashed.lock")).Dispose();

        var entry = Assert.Single(store.FindAbandoned());
        Assert.Equal(@"C:\Designs\App.forma", entry.ProjectPath);
        var restored = ProjectFile.RestoreProject(ProjectFile.Read(entry.DocumentPath));
        Assert.Equal("Unsaved work", restored.Forms[0].Title);

        store.Discard(entry.Id);
        Assert.Empty(store.FindAbandoned());
    }

    [Fact]
    public void ClearingRemovesTheAutosaveButKeepsTheSessionRunning()
    {
        var store = new RecoveryStore(_folder);
        using var session = store.Begin();
        session.Save(Design("Draft"), null);
        session.Clear();
        Assert.False(File.Exists(Path.Combine(_folder, session.Id + ".forma")));
        session.Save(Design("Draft 2"), null);
        Assert.True(File.Exists(Path.Combine(_folder, session.Id + ".forma")));
    }
}
