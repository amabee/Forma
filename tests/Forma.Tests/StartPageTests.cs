using Forma.Builder;
using Forma.Core.Controls;

namespace Forma.Tests;

public sealed class StartPageTests : IDisposable
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"forma-recent-{Guid.NewGuid():N}.json");

    public void Dispose() => File.Delete(_file);

    [Fact]
    public void TemplatesBuildValidDesignsWithTheirContent()
    {
        foreach (var template in ProjectTemplates.All)
        {
            var model = new BuilderViewModel(); model.CreateNew();
            Assert.True(ProjectTemplates.Apply(model, template.Id));
            Assert.NotEmpty(model.Form!.Children);
            Assert.False(model.CanUndo);
            // Round-trips through the project validator.
            ProjectFile.RestoreProject(ProjectFile.Parse(ProjectFile.Serialize(model.CaptureProject())));
        }
        var login = new BuilderViewModel(); login.CreateNew();
        ProjectTemplates.Apply(login, "login");
        Assert.Equal("Sign in", login.Form!.Title);
        Assert.Equal(420, login.Appearance[login.Form.Id].Width);
        var user = login.Form.Children.OfType<TextBox>().First(control => control is not PasswordBox);
        Assert.Equal("Username", login.Appearance[user.Id].Placeholder);
        Assert.Contains(login.Form.Children, control => control is Button { Text: "Sign in" });

        var settings = new BuilderViewModel(); settings.CreateNew();
        ProjectTemplates.Apply(settings, "settings");
        Assert.Equal(["Light", "Dark", "System"], settings.ProjectControls.OfType<ComboBox>().Single().Items);

        var dashboard = new BuilderViewModel(); dashboard.CreateNew();
        ProjectTemplates.Apply(dashboard, "dashboard");
        var header = dashboard.Form!.Children.OfType<Panel>().Single();
        Assert.Equal(("top", "bottom"), (dashboard.Appearance[header.Id].Dock, dashboard.Appearance[header.Id].BorderSides));
        Assert.Equal([220, 440, 660], dashboard.Form.Children.OfType<Card>().Select(card => card.X ?? 0));
        Assert.False(ProjectTemplates.Apply(dashboard, "unknown"));
    }

    [Fact]
    public void RecentProjectsKeepNewestFirstWithoutDuplicatesAndOnlyKnownPaths()
    {
        var recent = new RecentProjects(_file);
        Assert.Empty(recent.Load());
        for (var index = 0; index < RecentProjects.Limit + 2; index++) recent.Add($@"C:\Designs\App{index}.forma");
        recent.Add(@"C:\Designs\App3.forma");
        var list = recent.Load();
        Assert.Equal(RecentProjects.Limit, list.Count);
        Assert.Equal(@"C:\Designs\App3.forma", list[0]);
        Assert.Single(list, path => path.EndsWith("App3.forma"));
        Assert.True(recent.Contains(@"c:\designs\app3.forma"));
        Assert.False(recent.Contains(@"C:\Windows\win.ini"));
        recent.Remove(@"C:\Designs\App3.forma");
        Assert.False(recent.Contains(@"C:\Designs\App3.forma"));
    }
}
