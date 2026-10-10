using Forma.Core.Controls;
using Control = Forma.Core.Controls.Control;
using Image = Forma.Core.Controls.Image;
using System.Text.Json.Serialization;

namespace Forma.Builder;

public sealed record ExplorerNode(
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("controlId")] string? ControlId = null,
    [property: JsonPropertyName("command")] string? Command = null,
    [property: JsonPropertyName("locked")] bool Locked = false,
    [property: JsonPropertyName("children")] ExplorerNode[]? Children = null,
    [property: JsonPropertyName("itemId")] string? ItemId = null);

/// <summary>A logical project tree, independent of editing folders and renderer state.</summary>
public static class ProjectExplorerService
{
    public static ExplorerNode Build(BuilderViewModel model)
    {
        ExplorerNode Sources(Control control) => new($"sources:{control.Id}", "Source files", "folder", Children: [
            new($"css:{control.Id}", "component.css", "css", control.Id, "view-css", model.Appearance[control.Id].Locked),
            new($"script:{control.Id}", "script.js", "javascript", control.Id, "view-script", model.Appearance[control.Id].Locked),
            new($"properties:{control.Id}", "custom-properties.json", "json", control.Id, "view-custom-properties", model.Appearance[control.Id].Locked)
        ]);
        ExplorerNode Component(Control control) => new(control.Id, control.Name ?? control.ControlType, control.ControlType == "form" ? "form" : "component",
            control.Id, Locked: model.Appearance[control.Id].Locked,
            Children: control.Children.Count == 0 && control.ControlType != "form" ? Sources(control).Children : [Sources(control), .. control.Children.Select(Component)]);
        ExplorerNode File(ProjectEntry entry) => new($"file:{entry.Id}", entry.Name,
            entry.IsFolder ? "project-folder" : Path.GetExtension(entry.Name).ToLowerInvariant() switch { ".css" => "css", ".json" => "json", _ => "javascript" },
            Command: entry.IsFolder ? null : "open-project-file", Children: entry.IsFolder ? model.Files.Where(file => file.ParentId == entry.Id).Select(File).ToArray() : null, ItemId: entry.Id);
        var assets = model.ProjectControls.OfType<Image>().Where(image => !string.IsNullOrEmpty(image.Source))
            .Select(image => new ExplorerNode($"asset:{image.Id}", AssetName(image), "image", image.Id, Locked: model.Appearance[image.Id].Locked)).ToArray();
        return new($"project:{model.ProjectRoot.Id}", model.ProjectPath is null ? "Untitled project" : Path.GetFileNameWithoutExtension(model.ProjectPath), "project", Children: [
            new("forms", "Forms", "folder", Children: model.Forms.Select(Component).ToArray()),
            new("files", "Files", "folder", Children: model.Files.Where(file => file.ParentId is null).Select(File).ToArray()),
            new("global-script", "main.js", "javascript", model.ProjectRoot.Id, "edit-global-script"),
            new("assets", $"Assets ({assets.Length})", "folder", Children: assets)
        ]);
    }

    private static string AssetName(Image image)
    {
        var name = image.Source.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ? "Embedded image"
            : Uri.TryCreate(image.Source, UriKind.Absolute, out var uri) ? Path.GetFileName(uri.IsFile ? uri.LocalPath : uri.AbsolutePath)
            : Path.GetFileName(image.Source);
        return $"{image.Name ?? image.ControlType} — {(string.IsNullOrEmpty(name) ? "Image" : name)}";
    }
}
