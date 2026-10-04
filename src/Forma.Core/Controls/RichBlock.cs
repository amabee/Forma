namespace Forma.Core.Controls;

public sealed record RichBlock(string Kind = "paragraph", RichRun[]? Runs = null);
