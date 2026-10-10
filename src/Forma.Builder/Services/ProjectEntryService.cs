namespace Forma.Builder;

/// <summary>Portable project files. Names never become arbitrary filesystem paths.</summary>
public static class ProjectEntryService
{
    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase) { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };
    public sealed record ModuleSource(string Path, string Content);
    public static string FilePath(IReadOnlyList<ProjectEntry> files, ProjectEntry entry)
    {
        var parts = new List<string> { entry.Name };
        var parent = entry.ParentId;
        while (parent is not null) { var folder = files.Single(file => file.Id == parent); parts.Insert(0, folder.Name); parent = folder.ParentId; }
        return string.Join("/", parts);
    }
    public static ModuleSource[] ModuleSources(IReadOnlyList<ProjectEntry> files) => files.Where(file => !file.IsFolder)
        .Select(file => new ModuleSource(FilePath(files, file), file.Content)).ToArray();

    public static string ValidateName(string name, bool folder)
    {
        if (name is null) throw new ArgumentException("A name is required.");
        name = name.Trim();
        if (name.Length is < 1 or > 120 || name is "." or ".." || name.EndsWith('.')
            || name.Any(character => character < 32 || "<>:\"/\\|?*".Contains(character))
            || Reserved.Contains(name.Split('.')[0])) throw new ArgumentException("Use a valid file or folder name without path separators.");
        if (!folder && Path.GetExtension(name).ToLowerInvariant() is not (".js" or ".css" or ".json"))
            throw new ArgumentException("Project files currently support .js, .css and .json.");
        return name;
    }

    public static void Validate(IReadOnlyList<ProjectEntry>? files)
    {
        if (files is null || files.Count > 1000) throw new InvalidDataException("A project supports up to 1000 file/folder entries.");
        var ids = new HashSet<string>(); var siblings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files) {
            if (file is null || string.IsNullOrEmpty(file.Id) || file.Id.Length > 128 || file.Id.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_') || !ids.Add(file.Id)) throw new InvalidDataException("Invalid or duplicate file ID.");
            try { if (ValidateName(file.Name, file.IsFolder) != file.Name) throw new ArgumentException("Invalid name spacing."); }
            catch (ArgumentException error) { throw new InvalidDataException("Invalid project file name.", error); }
            if (file.Content is null || file.Content.Length > 200_000 || file.IsFolder && file.Content.Length > 0) throw new InvalidDataException("Invalid file content; each file is limited to 200,000 characters.");
            if (!siblings.Add($"{file.ParentId ?? ""}/{file.Name}")) throw new InvalidDataException("Duplicate names in a folder.");
        }
        var lookup = files.ToDictionary(file => file.Id);
        foreach (var file in files) {
            var visited = new HashSet<string> { file.Id }; var parent = file.ParentId;
            while (parent is not null) {
                if (!lookup.TryGetValue(parent, out var folder) || !folder.IsFolder || !visited.Add(parent) || visited.Count > 32) throw new InvalidDataException("Invalid folder hierarchy.");
                parent = folder.ParentId;
            }
        }
    }

    public static ProjectEntry Add(List<ProjectEntry> files, string? parentId, string name, bool folder)
    {
        name = ValidateName(name, folder);
        var entry = new ProjectEntry { Name = name, ParentId = parentId, IsFolder = folder,
            Content = folder ? "" : Path.GetExtension(name).ToLowerInvariant() == ".json" ? "{}\n" : "" };
        var next = files.Append(entry).ToArray(); Validate(next); files.Add(entry); return entry;
    }
    public static void Rename(List<ProjectEntry> files, string id, string name)
    {
        var entry = files.Single(file => file.Id == id); name = ValidateName(name, entry.IsFolder);
        if (!entry.IsFolder && !string.Equals(Path.GetExtension(name), Path.GetExtension(entry.Name), StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Keep the file's extension when renaming.");
        var previous = entry.Name; entry.Name = name;
        try { Validate(files); } catch { entry.Name = previous; throw; }
    }
    public static string[] Descendants(List<ProjectEntry> files, string id)
    {
        if (!files.Any(file => file.Id == id)) throw new ArgumentException("File not found.");
        var result = new HashSet<string> { id };
        bool changed; do { changed = false; foreach (var file in files) if (file.ParentId is not null && result.Contains(file.ParentId)) changed |= result.Add(file.Id); } while (changed);
        return result.ToArray();
    }
    public static void SetContent(List<ProjectEntry> files, string id, string content)
    {
        var entry = files.Single(file => file.Id == id);
        if (entry.IsFolder || content.Length > 200_000) throw new ArgumentException("Invalid file content.");
        entry.Content = content;
    }
}
