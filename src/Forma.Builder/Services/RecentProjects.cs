using System.Text.Json;

namespace Forma.Builder;

/// <summary>The most recently opened or saved project files, newest first, for the start page.</summary>
public sealed class RecentProjects(string file)
{
    public const int Limit = 8;

    public static RecentProjects Default { get; } = new(Path.Combine(FormaPaths.DataFolder, "recent-projects.json"));

    public IReadOnlyList<string> Load()
    {
        try
        {
            var paths = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(file)) ?? [];
            return paths.Where(path => !string.IsNullOrWhiteSpace(path) && Path.IsPathFullyQualified(path))
                .Distinct(StringComparer.OrdinalIgnoreCase).Take(Limit).ToList();
        }
        catch (Exception error) when (error is IOException or JsonException or UnauthorizedAccessException) { return []; }
    }

    public void Add(string path) => Save([Path.GetFullPath(path), .. Load().Where(item => !string.Equals(item, Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase))]);

    public void Remove(string path) => Save(Load().Where(item => !string.Equals(item, path, StringComparison.OrdinalIgnoreCase)));

    /// <summary>True only for paths the user actually opened or saved, so page messages can't name arbitrary files.</summary>
    public bool Contains(string path) => Load().Contains(path, StringComparer.OrdinalIgnoreCase);

    private void Save(IEnumerable<string> paths)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllText(file, JsonSerializer.Serialize(paths.Take(Limit).ToList()));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
    }
}
