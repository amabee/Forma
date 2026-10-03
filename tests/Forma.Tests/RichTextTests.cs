using System.Text.Json;
using Forma.Core.Controls;
using Forma.Core.Rendering;
using Forma.WebView2;
using Forma.Builder;

namespace Forma.Tests;
public class RichTextTests
{
    [Fact]
    public void RichDocumentsAreCopiedAndProducePlainText()
    {
        var runs = new[] { new RichRun("Hello", Bold: true) }; var rich = new RichTextBox { Document = [new("paragraph", runs), new("bullet", [new("World")])] };
        runs[0] = new("Changed"); Assert.Equal("Hello\nWorld", rich.Text);
        var copy = rich.Document; copy[0].Runs![0] = new("Changed"); Assert.True(rich.Document[0].Runs![0].Bold);
        rich.SetPlainText("A\nB"); Assert.Equal(2, rich.Document.Length); Assert.False(rich.Document[0].Runs![0].Bold);
    }
    [Fact]
    public async Task StructuredEditorMessagesAreValidatedAndRespectReadOnly()
    {
        var bridge = new FakeBridge(); var renderer = new WebView2Renderer(bridge); var rich = new RichTextBox(); await renderer.RenderAsync(rich);
        void Send(object document) => bridge.Receive(new BridgeMessage { Type = "event", Id = rich.Id, Event = "rich-input", Payload = JsonSerializer.SerializeToElement(new { document }) });
        Send(new[] { new { kind = "bullet", runs = new[] { new { text = "Edited", bold = true, italic = false, underline = true } } } });
        Assert.Equal("Edited", rich.Text); Assert.True(rich.Document[0].Runs![0].Underline);
        rich.ReadOnly = true; Send(new[] { new { kind = "paragraph", runs = new[] { new { text = "Ignored" } } } }); Assert.Equal("Edited", rich.Text);
        rich.ReadOnly = false; Send(new[] { new { kind = "paragraph", runs = (object?)null } }); Assert.Equal("Edited", rich.Text);
    }
    [Fact]
    public void RichFormattingAndPictureBoxRoundTripInFormaFiles()
    {
        var form = new Forma.Core.Form(); var rich = new RichTextBox { Document = [new("number", [new("Title", true, true, true)])] }; form.Add(rich);
        form.Add(new PictureBox { Source = "data:image/png;base64,AQID", SizeMode = "cover" });
        var loaded = ProjectFile.Restore(ProjectFile.Capture(form, _ => JsonSerializer.SerializeToElement(new { Width = 640, Height = 440 }))).Form;
        var document = Assert.IsType<RichTextBox>(loaded.Children[0]).Document;
        Assert.Equal("number", document[0].Kind); Assert.True(document[0].Runs![0].Italic);
        Assert.Equal("cover", Assert.IsType<PictureBox>(loaded.Children[1]).SizeMode);
    }
}
