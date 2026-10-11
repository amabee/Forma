namespace Forma.Builder;

/// <summary>Where the Builder keeps per-user data (autosaves, recent projects).</summary>
public static class FormaPaths
{
    /// <summary>
    /// %LOCALAPPDATA%\Forma, or the folder in the FORMA_DATA_DIR environment variable, which
    /// lets tests and portable setups keep their data apart from the user's own.
    /// </summary>
    public static string DataFolder =>
        Environment.GetEnvironmentVariable("FORMA_DATA_DIR") is { Length: > 0 } custom
            ? custom
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Forma");
}
