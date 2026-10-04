namespace Forma.Core.Controls;

public sealed class FilePicker : PathPicker
{
    private string _filter = "All files|*.*";
    public FilePicker() { Text = "Browse…"; DialogTitle = "Choose file"; }
    public string Filter
    {
        get => _filter;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            var parts = value.Split('|');
            if (value.Length > 8192 || parts.Length % 2 != 0 || parts.Any(string.IsNullOrWhiteSpace)
                || value.Contains('\0')) throw new ArgumentException("Use label|pattern filter pairs, such as Images|*.png;*.jpg.");
            SetProperty(ref _filter, value);
        }
    }
}
