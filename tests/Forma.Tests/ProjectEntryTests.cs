using System.Text.Json;
using Forma.Builder;

namespace Forma.Tests;

public class ProjectEntryTests
{
    [Fact]
    public void FilesAndFoldersPersistWithAllFormsAndParticipateInHistory()
    {
        var model = new BuilderViewModel(); model.CreateNew(); model.AddForm(); model.ClearHistory();
        var folder = model.EditProjectFiles(files => ProjectEntryService.Add(files, null, "Scripts", true));
        var file = model.EditProjectFiles(files => ProjectEntryService.Add(files, folder.Id, "helpers.js", false));
        model.EditProjectFiles(files => { ProjectEntryService.SetContent(files, file.Id, "const count = 1;"); return true; });
        var document = model.CaptureProject(); Assert.Equal(3, document.Version);
        var restored = ProjectFile.RestoreProject(ProjectFile.Parse(ProjectFile.Serialize(document)));
        Assert.Equal(2, restored.Forms.Count); Assert.Equal(2, restored.Files.Count);
        Assert.Equal(folder.Id, restored.Files.Single(entry => entry.Id == file.Id).ParentId);
        Assert.Equal("const count = 1;", restored.Files.Single(entry => entry.Id == file.Id).Content);
        var before = ProjectFile.RestoreProject(ProjectFile.Parse(model.Undo()!.Json));
        Assert.Equal("", before.Files.Single(entry => entry.Id == file.Id).Content);
        Assert.Equal("const count = 1;", ProjectFile.RestoreProject(ProjectFile.Parse(model.Redo()!.Json)).Files.Single(entry => entry.Id == file.Id).Content);
        Assert.True(model.HasUnsavedChanges);
        model.CreateNew(); Assert.Empty(model.Files);
    }

    [Theory]
    [InlineData("../bad.js")]
    [InlineData("C:\\bad.js")]
    [InlineData("CON.js")]
    [InlineData("script.exe")]
    [InlineData(".")]
    [InlineData("bad?.js")]
    public void InvalidNamesCannotBecomeEditingPaths(string name)
        => Assert.Throws<ArgumentException>(() => ProjectEntryService.Add([], null, name, false));

    [Fact]
    public void DuplicateNamesAndInvalidFolderLinksAreRejectedWithoutPartialEdits()
    {
        var files = new List<ProjectEntry>();
        var first = ProjectEntryService.Add(files, null, "first.js", false);
        var second = ProjectEntryService.Add(files, null, "second.js", false);
        Assert.Throws<InvalidDataException>(() => ProjectEntryService.Rename(files, second.Id, "FIRST.js"));
        Assert.Equal("second.js", second.Name);
        Assert.Throws<InvalidDataException>(() => ProjectEntryService.Add(files, first.Id, "child.js", false));
        Assert.Equal(2, files.Count);
        var folder = ProjectEntryService.Add(files, null, "Folder", true);
        folder.ParentId = folder.Id;
        Assert.Throws<InvalidDataException>(() => ProjectEntryService.Validate(files));
    }

    [Fact]
    public void FolderRemovalContainsOnlyItsDescendantsAndMalformedSavedFilesAreRejected()
    {
        var model = new BuilderViewModel(); model.CreateNew();
        var root = ProjectEntryService.Add(model.Files, null, "Scripts", true);
        var child = ProjectEntryService.Add(model.Files, root.Id, "data.json", false);
        var other = ProjectEntryService.Add(model.Files, null, "styles.css", false);
        Assert.Equal(new[] { root.Id, child.Id }.Order(), ProjectEntryService.Descendants(model.Files, root.Id).Order());
        var document = model.CaptureProject(); document.Files[1].ParentId = "missing";
        Assert.Throws<InvalidDataException>(() => ProjectFile.RestoreProject(document));
        Assert.Equal("{}\n", child.Content);
        Assert.Throws<ArgumentException>(() => ProjectEntryService.Rename(model.Files, other.Id, "styles.js"));
    }

    [Fact]
    public void RemovingFirstFormRetainsGlobalScriptFilesAndIsUndoable()
    {
        var model = new BuilderViewModel(); model.CreateNew(); var firstId = model.Form.Id;
        model.GlobalScript = "// project global";
        ProjectEntryService.Add(model.Files, null, "helpers.js", false);
        model.AddForm(); model.RemoveForm(firstId);
        Assert.Single(model.Forms); Assert.Single(model.Files); Assert.Equal("// project global", model.GlobalScript);
        var restored = ProjectFile.RestoreProject(ProjectFile.Parse(model.Undo()!.Json));
        Assert.Equal(2, restored.Forms.Count); Assert.Single(restored.Files);
        Assert.Throws<InvalidOperationException>(() => model.RemoveForm(model.Form.Id));
    }
}
