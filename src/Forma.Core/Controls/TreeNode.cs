namespace Forma.Core.Controls;

public sealed record TreeNode(string Id, string Text, TreeNode[]? Children = null);
