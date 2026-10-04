namespace Forma.Core.Controls;

public sealed class DataGridView : Control
{
    private string[] _columns = ["Name", "Value"];
    private string[][] _rows =
    [
        ["First row", "1"],
        ["Second row", "2"],
    ];
    private bool _readOnly = true;
    public string[] Columns
    {
        get => (string[])_columns.Clone();
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            SetProperty(ref _columns, (string[])value.Clone());
        }
    }
    public string[][] Rows
    {
        get => _rows.Select(row => (string[])row.Clone()).ToArray();
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            SetProperty(ref _rows, value.Select(row => (string[])row.Clone()).ToArray());
        }
    }
    public bool ReadOnly
    {
        get => _readOnly;
        set => SetProperty(ref _readOnly, value);
    }
    public event EventHandler<CellChangedEventArgs>? CellChanged;

    public void SetCell(int row, int column, string value)
    {
        if (ReadOnly || row < 0 || row >= _rows.Length || column < 0 || column >= _columns.Length)
            return;
        var cells = _rows[row];
        if (cells.Length < _columns.Length)
            Array.Resize(ref cells, _columns.Length);
        if (cells[column] == value)
            return;
        cells[column] = value;
        _rows[row] = cells;
        OnPropertyChanged(nameof(Rows));
        CellChanged?.Invoke(this, new(row, column, value));
    }
}
