namespace Forma.Builder;

// Start page: recent projects and starter templates, shown on launch and from File > Start page.
public sealed partial class BuilderWindow
{
    private Task SendStartPageAsync()
    {
        if (_bridge is null) return Task.CompletedTask;
        var recent = RecentProjects.Default.Load().Select(path => new
        {
            path,
            name = Path.GetFileNameWithoutExtension(path),
            folder = Path.GetDirectoryName(path) ?? "",
            exists = File.Exists(path),
        }).ToArray();
        return _bridge.SendAsync(new
        {
            type = "designer",
            action = "start-page",
            recent,
            templates = ProjectTemplates.All.Select(template => new { id = template.Id, name = template.Name, description = template.Description }).ToArray(),
        });
    }

    private async Task OpenRecentAsync(string? path)
    {
        // Only paths the user opened or saved before; the page cannot name arbitrary files.
        if (path is null || !RecentProjects.Default.Contains(path)) return;
        if (!File.Exists(path))
        {
            RecentProjects.Default.Remove(path);
            await StateAsync($"Could not find {Path.GetFileName(path)}; removed it from recent projects");
            await SendStartPageAsync();
            return;
        }
        try
        {
            if (await LoadProjectAsync(path, path, saved: true))
            {
                RecentProjects.Default.Add(path);
                await StateAsync($"Opened {Path.GetFileName(path)}");
            }
        }
        catch (Exception error)
        {
            _ready = true;
            MessageBox.Show(this, error.Message, "Could not open design", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task NewFromTemplateAsync(string? id)
    {
        var template = ProjectTemplates.All.FirstOrDefault(item => item.Id == id);
        if (template is null || _renderer is null || !ConfirmDiscard()) return;
        CloseDockedEditor();
        _ready = false;
        try
        {
            if (_viewModel.Form is not null) await _renderer.RemoveAsync(_viewModel.Form);
            DisposeComponents();
            ProjectTemplates.Apply(_viewModel, template.Id);
            _viewModel.ProjectPath = null;
            await RenderActiveFormAsync();
        }
        finally { _ready = true; }
        await StateAsync($"Created from the {template.Name} template. Save to keep it.");
    }
}
