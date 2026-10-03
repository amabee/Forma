using System.Text;

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

public sealed class MaskedTextBox : TextBox
{
    private string _mask = "000-0000";
    public string Mask { get => _mask; set { if (value is null || value.Length > 128) throw new ArgumentException("Mask must contain at most 128 characters."); SetProperty(ref _mask, value); SetMaskedText(Text ?? ""); } }
    public bool MaskCompleted {
        get { var formatted = Format(Text ?? ""); return Mask.Select((token, i) => token is not ('0' or 'L' or 'A' or '*') || formatted[i] != '_').All(complete => complete); }
    }
    public void SetMaskedText(string text) => SetText(Format(text));
    public string Format(string text)
    {
        if (Mask.Length == 0) return text;
        var result = new StringBuilder(); var index = 0;
        foreach (var token in Mask) {
            if (token is not ('0' or 'L' or 'A' or '*')) { result.Append(token); if (index < text.Length && text[index] == token) index++; continue; }
            char next = '_';
            while (index < text.Length) {
                var candidate = text[index++];
                if (candidate == '_') break;
                var match = token switch { '0' => char.IsAsciiDigit(candidate), 'L' => char.IsAsciiLetter(candidate), 'A' => char.IsAsciiLetterOrDigit(candidate), _ => candidate != '_' };
                if (match) { next = candidate; break; }
            }
            result.Append(next);
        }
        return result.ToString();
    }
}

public sealed class ItemCheckEventArgs(int index, bool isChecked) : EventArgs
{
    public int Index { get; } = index;
    public bool Checked { get; } = isChecked;
}
public sealed class CheckedListBox : Control
{
    private string[] _items = ["Item 1", "Item 2", "Item 3"];
    private int[] _checkedIndices = [];
    public string[] Items { get => (string[])_items.Clone(); set { ArgumentNullException.ThrowIfNull(value); SetProperty(ref _items, (string[])value.Clone()); CheckedIndices = _checkedIndices; } }
    public int[] CheckedIndices { get => (int[])_checkedIndices.Clone(); set { ArgumentNullException.ThrowIfNull(value); var next = value.Where(i => i >= 0 && i < _items.Length).Distinct().Order().ToArray(); if (!_checkedIndices.SequenceEqual(next)) SetProperty(ref _checkedIndices, next); } }
    public event EventHandler<ItemCheckEventArgs>? ItemCheck;
    public void SetItemChecked(int index, bool value) {
        if (index < 0 || index >= _items.Length || _checkedIndices.Contains(index) == value) return;
        CheckedIndices = value ? [.._checkedIndices, index] : _checkedIndices.Where(i => i != index).ToArray();
        ItemCheck?.Invoke(this, new(index, value));
    }
}
