using System.Text;

namespace Forma.Core.Controls;

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
