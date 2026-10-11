using System.Text.Json;
using Forma.Builder;

namespace Forma.Tests;

public class PreviewProjectTests
{
    [Fact]
    public void PreviewOpensAnyFormFromASnapshotTakenAtStart()
    {
        var design = new BuilderViewModel(); design.CreateNew();
        var main = design.Form!;
        design.GlobalScript = "window.started = true;";
        design.AddForm();
        var settings = design.Form!;
        design.ExecuteEdit("drop", settings.Id, JsonSerializer.SerializeToElement(new { control = "button", x = 10, y = 10 }));
        design.SelectForm(main.Id);

        var project = PreviewProject.Start(design);
        Assert.Equal(main.Id, project.StartFormId);
        Assert.Equal(["form1", "form2"], project.Forms.Select(form => form.Name));

        // Designer edits after Preview started do not reach the running app.
        design.SelectForm(settings.Id);
        design.ExecuteEdit("drop", settings.Id, JsonSerializer.SerializeToElement(new { control = "label", x = 10, y = 80 }));

        using var opened = project.Open("FORM2");
        Assert.Equal(settings.Id, opened.Form.Id);
        Assert.Single(opened.Form.Children);
        Assert.Equal("window.started = true;", opened.Appearance[opened.Form.Id].GlobalScript);
        // Each window gets its own copy, so two windows of one form don't share controls.
        using var again = project.Open(settings.Id);
        Assert.NotSame(opened.Form, again.Form);
        Assert.Throws<ArgumentException>(() => project.Open("missing"));
    }

    [Fact]
    public void SharedValuesAreKeptForTheRunAndReachEveryOtherWindow()
    {
        var design = new BuilderViewModel(); design.CreateNew();
        var project = PreviewProject.Start(design);
        var changes = new List<PreviewProject.SharedChange>();
        project.SharedChanged += (_, change) => changes.Add(change);
        var window = new object();
        project.SetShared("count", JsonSerializer.SerializeToElement(5), window);
        Assert.Equal(5, project.SharedState["count"].GetInt32());
        Assert.Same(window, Assert.Single(changes).Source);

        // A window opened later receives the current values with its first state.
        using var later = project.Open(project.StartFormId);
        var state = JsonSerializer.SerializeToElement(later.State());
        Assert.Equal(5, state.GetProperty("sharedState").GetProperty("count").GetInt32());

        project.SetShared("count", null, window);
        Assert.False(project.SharedState.ContainsKey("count"));
        project.SetShared("missing", null, window);
        Assert.Equal(2, changes.Count);
        Assert.Throws<ArgumentException>(() => project.SetShared("", JsonSerializer.SerializeToElement(1), window));
        Assert.Throws<ArgumentException>(() => project.SetShared("big", JsonSerializer.SerializeToElement(new string('x', 1_000_001)), window));
    }
}
