using Forma.Core.Rendering;
using System.Text.Json;

namespace Forma.Core.Controls;

/// <summary>Rich text stored as paragraphs and styled runs, rather than arbitrary HTML.</summary>
public sealed class RichTextBox : Control
{
    private RichBlock[] _document = [new("paragraph", [new("Write something...")])];
    private bool _readOnly;
    public RichTextBox() => base.Text = "Write something...";
    public override string? Text { get => base.Text; set => SetPlainText(value ?? ""); }
    public RichBlock[] Document
    {
        get => _document.Select(b => b with { Runs = (RichRun[])b.Runs!.Clone() }).ToArray();
        set {
            ArgumentNullException.ThrowIfNull(value);
            if (value.Length > 500 || value.Any(b => b is null || b.Runs is null || b.Runs.Any(r => r is null || r.Text is null))
                || value.Sum(b => (long)b.Runs!.Length) > 5000 || value.Sum(b => b.Runs!.Sum(r => (long)r.Text.Length)) > 1024 * 1024)
                throw new ArgumentException("Invalid rich text document or document limit exceeded.");
            var next = value.Select(b => new RichBlock(b.Kind is "bullet" or "number" ? b.Kind : "paragraph", (RichRun[])b.Runs!.Clone())).ToArray();
            if (_document.SequenceEqual(next)) return;
            SetProperty(ref _document, next); base.Text = string.Join("\n", next.Select(b => string.Concat(b.Runs!.Select(r => r.Text))));
            TextChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public bool ReadOnly { get => _readOnly; set => SetProperty(ref _readOnly, value); }
    public event EventHandler? TextChanged;
    public void SetPlainText(string text) => Document = text.Split('\n').Select(line => new RichBlock("paragraph", [new(line.TrimEnd('\r'))])).ToArray();

    protected override void WriteRenderState(IDictionary<string, object?> state)
    {
        base.WriteRenderState(state);
        state["document"] = Document.Select(b => new { kind = b.Kind, runs = b.Runs!.Select(r => new { text = r.Text, bold = r.Bold, italic = r.Italic, underline = r.Underline }).ToArray() }).ToArray();
        state["readOnly"] = ReadOnly;
    }

    protected override void OnRuntimeEvent(RuntimeEvent e)
    {
        base.OnRuntimeEvent(e);
        if (e.Name != "rich-input" || ReadOnly || !e.TryGetElement("document", out var document)) return;
        try { Document = document.Deserialize<RichBlock[]>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? []; }
        catch (Exception error) when (error is JsonException or ArgumentException) { /* Reject malformed editor messages. */ }
    }
}
