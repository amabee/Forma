namespace Forma.Core.Controls;

public sealed class LinkLabel : Control
{
    private string _url = "https://example.com/";
    private bool _visited;
    public string Url { get => _url; set { if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https") SetProperty(ref _url, uri.AbsoluteUri); } }
    public bool Visited { get => _visited; set => SetProperty(ref _visited, value); }
    public event EventHandler? LinkClicked;
    public void OnLinkClicked() { Visited = true; LinkClicked?.Invoke(this, EventArgs.Empty); }
}
