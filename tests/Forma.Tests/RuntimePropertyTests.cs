using System.Text.Json;
using Forma.Builder;
using Forma.Core.Controls;

namespace Forma.Tests;

public class RuntimePropertyTests
{
    private static JsonElement Payload(object value) => JsonSerializer.SerializeToElement(value);
    private static Control Add(BuilderViewModel model, string kind) => model.ExecuteEdit("drop", model.Form!.Id, Payload(new { control = kind, x = 0, y = 0 })).AddedControl!;
    [Fact]
    public void EveryCatalogEntryProjectsAValueAndAcceptsItsTypedDefaultOrRejectsReadOnlyWrites()
    {
        using var stream = typeof(RuntimePropertyService).Assembly.GetManifestResourceStream("Forma.RuntimeScriptProperties.json")!;
        var catalog = JsonDocument.Parse(stream).RootElement;
        var kinds = catalog.GetProperty("specific").EnumerateObject().Select(p => p.Name)
            .Concat(catalog.GetProperty("readOnly").EnumerateObject().Select(p => p.Name)).Append("form").Distinct();
        var model = new BuilderViewModel(); model.CreateNew();
        foreach (var kind in kinds)
        {
            var control = kind == "form" ? model.Form : Add(model, kind);
            var appearance = model.Appearance[control.Id]; var state = new Dictionary<string, object?>();
            RuntimePropertyService.Project(control, appearance, state);
            var readOnly = catalog.GetProperty("readOnly").TryGetProperty(kind, out var read) ? read.EnumerateArray().Select(v => v.GetString()!).ToArray() : [];
            foreach (var property in Assert.IsType<string[]>(state["scriptProperties"]))
            {
                Assert.True(state.ContainsKey(property), $"{kind}.{property} was advertised without a value.");
                if (readOnly.Contains(property)) Assert.Throws<ArgumentException>(() => RuntimePropertyService.TrySet(control, appearance, property, Payload(state[property]!)));
                else Assert.True(RuntimePropertyService.TrySet(control, appearance, property, Payload(state[property]!)), $"{kind}.{property} rejected its own default.");
            }
        }
    }

    [Fact]
    public void AppearanceInputAndDisplaySettersAreValidatedAndDoNotEditTheDesign()
    {
        var model = new BuilderViewModel(); model.CreateNew();
        var input = Add(model, "textbox"); var avatar = Add(model, "avatar"); var badge = Add(model, "badge");
        using var preview = new PreviewSession(model);
        preview.SetValue(input.Id, "placeholder", Payload("Your full name"));
        preview.SetValue(input.Id, "readOnly", Payload(true)); preview.SetValue(input.Id, "maxLength", Payload(40));
        preview.SetValue(input.Id, "fontSize", Payload(24)); preview.SetValue(input.Id, "foreColor", Payload("#123456"));
        preview.SetValue(input.Id, "width", Payload(250)); preview.SetValue(input.Id, "x", Payload(60));
        preview.SetValue(avatar.Id, "initials", Payload("AB")); preview.SetValue(avatar.Id, "shape", Payload("square"));
        preview.SetValue(badge.Id, "variant", Payload("success"));
        var text = Assert.IsType<TextBox>(preview.Controls.Single(c => c.Id == input.Id));
        Assert.True(text.ReadOnly); Assert.Equal(40, text.MaxLength); Assert.Equal(60, text.X);
        Assert.Equal(250, preview.Appearance[input.Id].Width); Assert.Equal(24, preview.Appearance[input.Id].FontSize);
        Assert.False(model.Appearance[input.Id].ReadOnly); Assert.NotEqual(250, model.Appearance[input.Id].Width);
        Assert.Equal("square", preview.Controls.OfType<Avatar>().Single().Shape);
        Assert.Throws<ArgumentException>(() => preview.SetValue(input.Id, "fontSize", Payload("24")));
        Assert.Throws<ArgumentException>(() => preview.SetValue(input.Id, "foreColor", Payload("bad")));
        Assert.Throws<ArgumentException>(() => preview.SetValue(avatar.Id, "shape", Payload("triangle")));
        Assert.Equal(24, preview.Appearance[input.Id].FontSize);
    }

    [Fact]
    public void StructuredDataUsesModelValidationAndTargetReferencesResolveByName()
    {
        var model = new BuilderViewModel(); model.CreateNew();
        var tree = Add(model, "treeview"); var rich = Add(model, "richtextbox"); var grid = Add(model, "propertygrid");
        var tip = Add(model, "tooltip"); var field = Add(model, "textbox");
        using var preview = new PreviewSession(model);
        preview.SetValue(tree.Id, "nodes", Payload(new[] { new { id = "a", text = "A", children = new[] { new { id = "b", text = "B" } } } }));
        preview.SetValue(tree.Id, "selectedNode", Payload("b")); preview.SetValue(tree.Id, "expandedNodes", Payload(new[] { "a" }));
        preview.SetValue(rich.Id, "document", Payload(new[] { new { kind = "paragraph", runs = new[] { new { text = "Hello", bold = true } } } }));
        preview.SetValue(grid.Id, "entries", Payload(new[] { new { name = "Title", value = "Runtime", category = "General" } }));
        preview.SetValue(tip.Id, "targetId", Payload(field.Name!));
        Assert.Equal(field.Id, preview.Controls.OfType<Tooltip>().Single().TargetId);
        Assert.Equal("b", preview.Controls.OfType<TreeView>().Single().SelectedNode);
        Assert.Equal("Hello", preview.Controls.OfType<RichTextBox>().Single().Text);
        Assert.Throws<ArgumentException>(() => preview.SetValue(tree.Id, "nodes", Payload(new[] { new { id = "a", text = "A" }, new { id = "a", text = "Duplicate" } })));
        Assert.Equal("b", preview.Controls.OfType<TreeView>().Single().SelectedNode);
        Assert.Throws<ArgumentException>(() => preview.SetValue(tip.Id, "targetId", Payload("missing")));
        Assert.Equal(field.Id, preview.Controls.OfType<Tooltip>().Single().TargetId);
    }

    [Fact]
    public void ScriptResizePreservesNestedAnchorsAndStructuredSnapshotsUseCamelCase()
    {
        var model = new BuilderViewModel(); model.CreateNew();
        var panel = Add(model, "panel");
        var child = model.ExecuteEdit("drop", panel.Id, Payload(new { control = "button", x = 30, y = 40 })).AddedControl!;
        model.Appearance[child.Id].Anchor = "bottom,right";
        var tree = Add(model, "treeview");
        using var preview = new PreviewSession(model);
        preview.SetValue(panel.Id, "width", Payload(model.Appearance[panel.Id].Width + 100));
        preview.SetValue(panel.Id, "height", Payload(model.Appearance[panel.Id].Height + 50));
        var runtimeChild = preview.Controls.Single(c => c.Id == child.Id);
        Assert.Equal(130, runtimeChild.X); Assert.Equal(90, runtimeChild.Y);
        Assert.Equal(30, child.X); Assert.Equal(40, child.Y);
        var state = Payload(preview.State()).GetProperty("controls").EnumerateArray().Single(c => c.GetProperty("id").GetString() == tree.Id);
        Assert.Equal("root", state.GetProperty("nodes")[0].GetProperty("id").GetString());
        Assert.True(state.GetProperty("nodes")[0].TryGetProperty("children", out _));
    }
}
