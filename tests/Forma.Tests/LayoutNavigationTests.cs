using System.Text.Json;
using Forma.Builder;
using Forma.Core.Controls;

namespace Forma.Tests;

public class LayoutNavigationTests
{
    private static JsonElement Payload(object value) => JsonSerializer.SerializeToElement(value);
    [Fact]
    public void AccordionSectionsAndResponsiveContainersPersistAndRuntimeEditsStayIsolated()
    {
        var model = new BuilderViewModel(); model.CreateNew();
        var accordion = Assert.IsType<Accordion>(model.ExecuteEdit("drop", model.Form.Id, Payload(new { control = "accordion", x = 0, y = 0 })).AddedControl);
        var first = model.ExecuteEdit("drop", accordion.Id, Payload(new { control = "label", x = 0, y = 0 })).AddedControl!;
        model.ExecuteEdit("command", accordion.Id, Payload(new { command = "add-tab" }));
        Assert.Equal("Section 3", accordion.Tabs[2]);
        var third = model.ExecuteEdit("drop", accordion.Id, Payload(new { control = "button", x = 0, y = 0 })).AddedControl!;
        Assert.Equal(1, first.LayoutSlot); Assert.Equal(3, third.LayoutSlot);
        foreach (var kind in new[] { "sidebar", "appshell", "responsivepanel" })
        {
            var parent = model.ExecuteEdit("drop", model.Form.Id, Payload(new { control = kind, x = 0, y = 0 })).AddedControl!;
            model.ExecuteEdit("drop", parent.Id, Payload(new { control = "button", x = 0, y = 0 }));
            Assert.Single(parent.Children);
        }
        var shell = model.Form.Children.OfType<AppShell>().Single();
        model.ExecuteEdit("select", shell.Id, default);
        model.ExecuteEdit("property", shell.Id, Payload(new { property = "breakpoint", value = 720 }));
        var undone = ProjectFile.Restore(ProjectFile.Parse(model.Undo()!.Json));
        Assert.Equal(600, undone.Form.Children.OfType<AppShell>().Single().Breakpoint);
        var saved = ProjectFile.Restore(ProjectFile.Parse(ProjectFile.Serialize(model.CaptureProject()))).Form;
        Assert.Equal(720, saved.Children.OfType<AppShell>().Single().Breakpoint);
        Assert.Equal("vertical", saved.Children.OfType<Sidebar>().Single().Orientation);
        Assert.Equal(3, saved.Children.OfType<Accordion>().Single().Children[1].LayoutSlot);
        using var preview = new PreviewSession(model);
        preview.SetValue(shell.Id, "breakpoint", Payload(420));
        preview.SetValue(accordion.Id, "tabs", Payload(new[] { "Only" }));
        preview.SetValue(accordion.Id, "expanded", Payload(false));
        Assert.True(accordion.Expanded);
        Assert.Equal(720, shell.Breakpoint); Assert.Equal(3, accordion.Tabs.Length);
        Assert.Contains("breakpoint", InspectorCatalog.ForKind("appshell").Select(p => p.Id));
        Assert.Contains("tabs", InspectorCatalog.ForKind("accordion").Select(p => p.Id));
    }
    [Fact]
    public void LayoutsKeepNestedChildrenOrderAndSurviveSaveOpenAndUndo()
    {
        var model = new BuilderViewModel(); model.CreateNew();
        foreach (var kind in new[] { "stackpanel", "hstack", "vstack", "wrappanel", "centerpanel", "scrollablepanel" }) {
            var parent = model.ExecuteEdit("drop", model.Form.Id, Payload(new { control = kind, x = 0, y = 0 })).AddedControl!;
            var child = model.ExecuteEdit("drop", parent.Id, Payload(new { control = "button", x = 5, y = 5 })).AddedControl!;
            var inserted = model.ExecuteEdit("drop", parent.Id, Payload(new { control = "label", x = 5, y = 5, index = 0 })).AddedControl!;
            Assert.Same(parent, child.Parent);
            if (parent is LinearLayout) Assert.Equal(inserted.Id, parent.Children[0].Id);
        }
        var stack = model.Form.Children.OfType<StackPanel>().First(parent => parent.GetType() == typeof(StackPanel));
        Assert.Equal("vertical", stack.Orientation);
        Assert.Equal("horizontal", model.Form.Children.OfType<HStack>().Single().Orientation);
        model.ExecuteEdit("select", stack.Id, default);
        model.ExecuteEdit("property", stack.Id, Payload(new { property = "gap", value = 21 }));
        var undone = ProjectFile.Restore(ProjectFile.Parse(model.Undo()!.Json)); Assert.Equal(8, undone.Form.Children.OfType<StackPanel>().First(parent => parent.GetType() == typeof(StackPanel)).Gap);
        model.Redo();
        var scroll = model.Form.Children.OfType<ScrollablePanel>().Single();
        model.ExecuteEdit("select", scroll.Id, default); model.ExecuteEdit("property", scroll.Id, Payload(new { property = "scrollDirection", value = "vertical" }));
        var largeChild = scroll.Children[0]; model.ExecuteEdit("select", largeChild.Id, default);
        model.ExecuteEdit("property", largeChild.Id, Payload(new { property = "width", value = 700 })); Assert.Equal(700, model.Appearance[largeChild.Id].Width);
        var saved = ProjectFile.Restore(ProjectFile.Parse(ProjectFile.Serialize(model.CaptureProject())));
        Assert.Equal(6, saved.Form.Children.Count); Assert.All(saved.Form.Children, parent => Assert.Equal(2, parent.Children.Count));
        Assert.Equal("vertical", saved.Form.Children.OfType<ScrollablePanel>().Single().ScrollDirection);
        Assert.Equal(21, saved.Form.Children.OfType<StackPanel>().First(parent => parent.GetType() == typeof(StackPanel)).Gap);
        using var preview = new PreviewSession(model); preview.SetValue(stack.Id, "gap", Payload(12)); preview.SetValue(stack.Id, "orientation", Payload("horizontal"));
        Assert.Equal(21, stack.Gap); Assert.Equal("vertical", stack.Orientation);
    }
    [Fact]
    public void NavigationPropertiesPersistAndRuntimeChoicesAreIndependent()
    {
        var model = new BuilderViewModel(); model.CreateNew();
        var breadcrumb = Assert.IsType<Breadcrumb>(model.ExecuteEdit("drop", model.Form.Id, Payload(new { control = "breadcrumb", x = 0, y = 0 })).AddedControl);
        var nav = Assert.IsType<SideNavigation>(model.ExecuteEdit("drop", model.Form.Id, Payload(new { control = "sidenavigation", x = 0, y = 0 })).AddedControl);
        nav.Items = ["Home", "Reports"]; nav.SelectedIndex = 1;
        var saved = ProjectFile.Restore(ProjectFile.Parse(ProjectFile.Serialize(model.CaptureProject()))).Form;
        Assert.Equal(2, saved.Children.OfType<Breadcrumb>().Single().SelectedIndex);
        Assert.Equal("vertical", saved.Children.OfType<SideNavigation>().Single().Orientation);
        Assert.Equal(new[] { "Home", "Reports" }, saved.Children.OfType<SideNavigation>().Single().Items);
        using var preview = new PreviewSession(model); preview.SetValue(nav.Id, "selectedIndex", Payload(0)); preview.SetValue(nav.Id, "items", Payload(new[] { "New" }));
        Assert.Equal(1, nav.SelectedIndex); Assert.Equal(2, nav.Items.Length);
        Assert.Contains("navigate", ComponentEditorService.Template(nav, model.Appearance[nav.Id]).Behavior);
        Assert.Contains("scrollDirection", InspectorCatalog.ForKind("scrollablepanel").Select(property => property.Id));
    }
}
