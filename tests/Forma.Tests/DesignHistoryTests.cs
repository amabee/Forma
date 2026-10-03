using System.Text.Json;
using Forma.Builder;
using Forma.Core.Controls;

namespace Forma.Tests;
public class DesignHistoryTests
{
    private static DesignSnapshot Snapshot(string value) => new(value, "selection");
    [Fact]
    public void UndoRedoAndNewEditDiscardTheRedoBranch()
    {
        var history = new DesignHistory();
        Assert.False(history.Record(Snapshot("a"), Snapshot("a"))); Assert.False(history.CanUndo);
        history.Record(Snapshot("a"), Snapshot("b")); history.Record(Snapshot("b"), Snapshot("c"));
        Assert.Equal("b", history.Undo()!.Json); Assert.Equal("c", history.Redo()!.Json);
        history.Undo(); history.Record(Snapshot("b"), Snapshot("d"));
        Assert.False(history.CanRedo); Assert.Equal("b", history.Undo()!.Json); Assert.Equal("a", history.Undo()!.Json);
        Assert.Null(history.Undo()); history.Clear(); Assert.False(history.CanRedo);
    }
    [Fact]
    public void ConsecutiveTypingGroupsOnlyTheSameFieldAndTimeWindow()
    {
        var history = new DesignHistory(); var time = DateTimeOffset.UtcNow;
        history.Record(Snapshot("a"), Snapshot("ab"), "text:one", time);
        history.Record(Snapshot("ab"), Snapshot("abc"), "text:one", time.AddMilliseconds(100));
        Assert.Equal("a", history.Undo()!.Json); Assert.Equal("abc", history.Redo()!.Json);
        history.Record(Snapshot("abc"), Snapshot("abcd"), "text:one", time.AddSeconds(2));
        Assert.Equal("abc", history.Undo()!.Json);
    }
    [Fact]
    public void TypingBackToTheOriginalValueRemovesTheGroupedEdit()
    {
        var history = new DesignHistory(); var time = DateTimeOffset.UtcNow;
        history.Record(Snapshot("a"), Snapshot("b"), "text", time);
        history.Record(Snapshot("b"), Snapshot("a"), "text", time.AddMilliseconds(50));
        Assert.False(history.CanUndo);
    }
    [Fact]
    public void UndoRestoresDeletedNestedControlsAndSelectionWithStableIds()
    {
        var form = new Forma.Core.Form(); var panel = new Panel(); var button = new Button { Text = "Save", X = 30, Y = 40 };
        form.Add(panel); panel.Add(button);
        string Capture() => ProjectFile.Serialize(ProjectFile.Capture(form, _ => JsonSerializer.SerializeToElement(new { Width = 640, Height = 440 })));
        var before = new DesignSnapshot(Capture(), button.Id); form.Remove(panel);
        var history = new DesignHistory(); history.Record(before, new(Capture(), form.Id));
        var undo = history.Undo()!; var restored = ProjectFile.Restore(ProjectFile.Parse(undo.Json));
        var nested = Assert.IsType<Button>(restored.Form.Children[0].Children[0]);
        Assert.Equal(button.Id, nested.Id); Assert.Equal(button.Id, undo.SelectedId); Assert.Equal(30, nested.X);
        Assert.Empty(ProjectFile.Restore(ProjectFile.Parse(history.Redo()!.Json)).Form.Children);
    }
    [Fact]
    public void HistoryKeepsAtMostOneHundredEdits()
    {
        var history = new DesignHistory(); for (var i = 0; i < 150; i++) history.Record(Snapshot(i.ToString()), Snapshot((i + 1).ToString()));
        var count = 0; while (history.Undo() is not null) count++;
        Assert.Equal(100, count);
    }
}
