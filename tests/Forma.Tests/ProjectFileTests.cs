using System.Text.Json;
using Forma.Builder;
using Forma.Core.Controls;

namespace Forma.Tests;
public class ProjectFileTests
{
    private static JsonElement Appearance(Control _) => JsonSerializer.SerializeToElement(new { Width = 640, Height = 440, ZIndex = 12, CustomCss = "font-weight: bold" });
    [Fact]
    public void NestedDesignRestoresIdsPropertiesOrderAndAppearance()
    {
        var form = new Forma.Core.Form { Title = "My design", Name = "main" };
        var tabs = new TabControl { Tabs = ["A", "B"], SelectedTab = 1 }; form.Add(tabs);
        var choice = new ComboBox { Items = ["One", "Two"], SelectedIndex = 1, LayoutSlot = 2, X = 20, Y = 30 }; tabs.Add(choice);
        var grid = new DataGridView { Columns = ["Name"], Rows = [["Angel"]], ReadOnly = false }; form.Add(grid);
        var timer = new Forma.Core.Controls.Timer { Interval = 250 }; form.Add(timer);
        var json = ProjectFile.Serialize(ProjectFile.Capture(form, Appearance));
        var loaded = ProjectFile.Restore(ProjectFile.Parse(json));
        Assert.Equal(form.Id, loaded.Form.Id); Assert.Equal("My design", loaded.Form.Title);
        var restoredTabs = Assert.IsType<TabControl>(loaded.Form.Children[0]); Assert.Equal(1, restoredTabs.SelectedTab);
        var restoredChoice = Assert.IsType<ComboBox>(restoredTabs.Children[0]);
        Assert.Equal(choice.Id, restoredChoice.Id); Assert.Equal(1, restoredChoice.SelectedIndex); Assert.Equal(2, restoredChoice.LayoutSlot); Assert.Equal(20, restoredChoice.X);
        Assert.Equal("Angel", Assert.IsType<DataGridView>(loaded.Form.Children[1]).Rows[0][0]);
        Assert.Equal(250, Assert.IsType<Forma.Core.Controls.Timer>(loaded.Form.Children[2]).Interval);
        Assert.False(Assert.IsType<Forma.Core.Controls.Timer>(loaded.Form.Children[2]).Enabled);
        Assert.Equal(12, loaded.Appearance[choice.Id].GetProperty("ZIndex").GetInt32());
    }
    [Theory]
    [InlineData(150, 200, 170)]
    [InlineData(-100, -50, -75)]
    public void NumericBoundsRestoreBeforeValue(double minimum, double maximum, double value)
    {
        var form = new Forma.Core.Form(); var input = new NumericUpDown();
        input.Minimum = Math.Min(0, minimum); input.Maximum = maximum; input.Minimum = minimum; input.Value = value;
        form.Add(input);
        var loaded = ProjectFile.Restore(ProjectFile.Capture(form, Appearance));
        var restored = Assert.IsType<NumericUpDown>(loaded.Form.Children[0]);
        Assert.Equal(minimum, restored.Minimum); Assert.Equal(maximum, restored.Maximum); Assert.Equal(value, restored.Value);
    }
    [Fact]
    public void InvalidVersionKindAndDuplicateIdsAreRejected()
    {
        var form = new Forma.Core.Form(); form.Add(new Button());
        var project = ProjectFile.Capture(form, Appearance);
        project.Version = 2; Assert.Throws<InvalidDataException>(() => ProjectFile.Restore(project));
        project.Version = 1; project.Root.Children[0].Kind = "arbitrary-type"; Assert.Throws<InvalidDataException>(() => ProjectFile.Restore(project));
        project.Root.Children[0].Kind = "button"; project.Root.Children[0].Id = form.Id;
        Assert.Throws<InvalidDataException>(() => ProjectFile.Restore(project));
        Assert.Throws<JsonException>(() => ProjectFile.Parse("{broken"));
    }
    [Fact]
    public void LocalImagesAreEmbeddedAndAtomicWritesRoundTrip()
    {
        var folder = Path.Combine(Path.GetTempPath(), "forma-test-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try {
            var imagePath = Path.Combine(folder, "image.png"); File.WriteAllBytes(imagePath, [1, 2, 3]);
            var form = new Forma.Core.Form(); form.Add(new Image { Source = new Uri(imagePath).AbsoluteUri });
            var path = Path.Combine(folder, "design.forma"); ProjectFile.Write(path, ProjectFile.Capture(form, Appearance, true));
            File.Delete(imagePath);
            var loaded = ProjectFile.Restore(ProjectFile.Read(path));
            Assert.Equal("data:image/png;base64,AQID", Assert.IsType<Image>(loaded.Form.Children[0]).Source);
            var bad = ProjectFile.Capture(form, Appearance); bad.Version = 2;
            Assert.Throws<InvalidDataException>(() => ProjectFile.Write(path, bad));
            Assert.Equal(1, ProjectFile.Read(path).Version);
            Assert.Single(Directory.GetFiles(folder));
        } finally { foreach (var file in Directory.GetFiles(folder)) File.Delete(file); Directory.Delete(folder); }
    }
}
