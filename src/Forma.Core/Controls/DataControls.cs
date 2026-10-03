namespace Forma.Core.Controls;

public sealed class ListView : ChoiceControl { }
public sealed record TreeNode(string Id, string Text, TreeNode[]? Children = null);
public sealed class TreeView : Control
{
    private TreeNode[] _nodes = [new("root", "Root", [new("child", "Child")])];
    private string _selectedNode = "";
    private string[] _expandedNodes = ["root"];
    private static TreeNode Copy(TreeNode node) => node with { Children = (node.Children ?? []).Select(Copy).ToArray() };
    private static IEnumerable<TreeNode> Walk(IEnumerable<TreeNode> nodes) { foreach (var node in nodes) { yield return node; foreach (var child in Walk(node.Children ?? [])) yield return child; } }
    public TreeNode[] Nodes { get => _nodes.Select(Copy).ToArray(); set {
        ArgumentNullException.ThrowIfNull(value); var ids = new HashSet<string>(); var count = 0;
        void Validate(IEnumerable<TreeNode> nodes, int depth) { if (depth > 24) throw new ArgumentException("Tree is too deep."); foreach (var node in nodes) { if (node is null || string.IsNullOrWhiteSpace(node.Id) || node.Text is null || !ids.Add(node.Id) || ++count > 2000) throw new ArgumentException("Invalid or duplicate tree node."); Validate(node.Children ?? [], depth + 1); } }
        Validate(value, 0); SetProperty(ref _nodes, value.Select(Copy).ToArray()); SelectedNode = _selectedNode; ExpandedNodes = _expandedNodes;
    } }
    public string SelectedNode { get => _selectedNode; set { var next = Walk(_nodes).Any(n => n.Id == value) ? value : ""; if (next == _selectedNode) return; SetProperty(ref _selectedNode, next); SelectedNodeChanged?.Invoke(this, EventArgs.Empty); } }
    public string[] ExpandedNodes { get => (string[])_expandedNodes.Clone(); set { ArgumentNullException.ThrowIfNull(value); var ids = Walk(_nodes).Select(n => n.Id).ToHashSet(); var next = value.Where(ids.Contains).Distinct().ToArray(); if (!_expandedNodes.SequenceEqual(next)) SetProperty(ref _expandedNodes, next); } }
    public event EventHandler? SelectedNodeChanged;
    public void SetExpanded(string id, bool expanded) => ExpandedNodes = expanded ? [.._expandedNodes, id] : _expandedNodes.Where(n => n != id).ToArray();
}
public sealed class Pagination : Control
{
    private int _totalItems = 100, _pageSize = 10, _page = 1;
    public int TotalItems { get => _totalItems; set { SetProperty(ref _totalItems, Math.Max(0, value)); Page = _page; } }
    public int PageSize { get => _pageSize; set { SetProperty(ref _pageSize, Math.Clamp(value, 1, 10000)); Page = _page; } }
    public int PageCount => Math.Max(1, (int)Math.Ceiling((double)_totalItems / _pageSize));
    public int Page { get => _page; set { var next = Math.Clamp(value, 1, PageCount); if (next == _page) return; SetProperty(ref _page, next); PageChanged?.Invoke(this, EventArgs.Empty); } }
    public event EventHandler? PageChanged;
}
