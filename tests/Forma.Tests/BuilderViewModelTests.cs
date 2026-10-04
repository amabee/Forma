using System.Text.Json;
using Forma.Builder;
using Forma.Core.Controls;

namespace Forma.Tests;

public class BuilderViewModelTests
{
    [Fact]
    public void DirtyStateAndTitleNotifyAfterEditsAndSaving()
    {
        var model = new BuilderViewModel();
        var changed = new List<string?>();
        model.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        model.CreateNew();
        Assert.False(model.HasUnsavedChanges);
        model.Form!.Title = "My design";
        model.RefreshDocumentState();
        Assert.True(model.HasUnsavedChanges);
        Assert.StartsWith("* ", model.WindowTitle);
        Assert.Contains(nameof(model.HasUnsavedChanges), changed);
        Assert.Contains(nameof(model.WindowTitle), changed);
        model.ProjectPath = Path.Combine(Path.GetTempPath(), "Example.forma");
        model.MarkSaved();
        Assert.False(model.HasUnsavedChanges);
        Assert.Equal("Example.forma - Forma Builder", model.WindowTitle);
        model.Appearance[model.Form.Id].Width++;
        model.RefreshDocumentState();
        Assert.True(model.HasUnsavedChanges);
    }

    [Fact]
    public void PreviewBlocksHistoryCommandsWithoutConsumingThem()
    {
        var model = new BuilderViewModel();
        model.CreateNew();
        var before = model.CaptureHistory();
        model.Form!.Title = "Changed";
        model.RecordEdit(before, model.CaptureHistory());
        Assert.True(model.CanUndo);
        model.PreviewMode = true;
        Assert.False(model.CanUndo);
        Assert.Null(model.Undo());
        model.PreviewMode = false;
        Assert.Equal(before, model.Undo());
        Assert.True(model.CanRedo);
        Assert.NotNull(model.Redo());
    }

    [Fact]
    public void RestoredDocumentPreservesSelectionAppearanceAndSavedBaseline()
    {
        var model = new BuilderViewModel();
        model.CreateNew();
        var baseline = model.CaptureHistory();
        var button = new Button { Text = "Go" };
        model.Form!.Add(button);
        model.Appearance[button.Id] = new Appearance { Width = 180 };
        model.SelectedControl = button;
        var snapshot = model.CaptureHistory();
        model.RecordEdit(baseline, snapshot);
        var restored = ProjectFile.Restore(ProjectFile.Parse(snapshot.Json));
        model.ApplyDocument(restored.Form, restored.Appearance.ToDictionary(
            pair => pair.Key, pair => pair.Value.Deserialize<Appearance>()!), snapshot.SelectedId);
        Assert.Equal(button.Id, model.SelectedControl!.Id);
        Assert.Equal(180, model.Appearance[button.Id].Width);
        Assert.True(model.HasUnsavedChanges);
        Assert.True(model.CanUndo);
        model.CreateNew();
        Assert.Same(model.Form, model.SelectedControl);
        Assert.Single(model.Appearance);
        Assert.False(model.CanUndo);
        Assert.False(model.CanRedo);
        Assert.False(model.HasUnsavedChanges);
    }
}
