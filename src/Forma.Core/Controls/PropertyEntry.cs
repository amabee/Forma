namespace Forma.Core.Controls;

public sealed record PropertyEntry(string Name, string Value, string Category = "General", bool ReadOnly = false);
