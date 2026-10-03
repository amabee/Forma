using System.Text.Json;
using Forma.Builder;
using Forma.Core.Controls;
using Forma.Core.Rendering;
using Forma.WebView2;

namespace Forma.Tests;
public class DataControlsTests
{
    [Fact]
    public void TreesValidateIdsCopyNodesAndPruneRemovedSelections()
    {
        var tree = new TreeView { SelectedNode = "child" };
        var copy = tree.Nodes; copy[0].Children![0] = new("other", "Changed"); Assert.Equal("child", tree.Nodes[0].Children![0].Id);
        Assert.Throws<ArgumentException>(() => tree.Nodes = [new("x", "First"), new("x", "Duplicate")]);
        tree.Nodes = [new("new", "New")]; Assert.Equal("", tree.SelectedNode); Assert.Empty(tree.ExpandedNodes);
    }
    [Fact]
    public void PaginationHandlesEmptyTotalsAndClampsPages()
    {
        var pages = new Pagination { TotalItems = 21, PageSize = 10, Page = 3 }; Assert.Equal(3, pages.PageCount);
        pages.TotalItems = 0; Assert.Equal(1, pages.Page); Assert.Equal(1, pages.PageCount);
        pages.PageSize = 0; Assert.Equal(1, pages.PageSize);
    }
    [Fact]
    public async Task RendererRoutesTreeSelectionExpansionAndPages()
    {
        var bridge = new FakeBridge(); var renderer = new WebView2Renderer(bridge);
        var tree = new TreeView(); var pages = new Pagination(); await renderer.RenderAsync(tree); await renderer.RenderAsync(pages);
        void Send(Control control, string name, object payload) => bridge.Receive(new BridgeMessage { Type = "event", Id = control.Id, Event = name, Payload = JsonSerializer.SerializeToElement(payload) });
        Send(tree, "tree-select", new { node = "child" }); Assert.Equal("child", tree.SelectedNode);
        Send(tree, "tree-expand", new { node = "root", expanded = false }); Assert.Empty(tree.ExpandedNodes);
        Send(pages, "page", new { page = 100 }); Assert.Equal(10, pages.Page);
    }
    [Fact]
    public void NewDataControlsRetainStateInFormaFiles()
    {
        var form = new Forma.Core.Form(); form.Add(new TreeView { SelectedNode = "child", ExpandedNodes = ["root"] });
        form.Add(new Pagination { TotalItems = 250, PageSize = 25, Page = 8 }); form.Add(new ListView { Items = ["A", "B"], SelectedIndex = 1 });
        var restored = ProjectFile.Restore(ProjectFile.Capture(form, _ => JsonSerializer.SerializeToElement(new { Width = 640, Height = 440 }))).Form;
        Assert.Equal("child", Assert.IsType<TreeView>(restored.Children[0]).SelectedNode);
        Assert.Equal(8, Assert.IsType<Pagination>(restored.Children[1]).Page);
        Assert.Equal(1, Assert.IsType<ListView>(restored.Children[2]).SelectedIndex);
    }
}
