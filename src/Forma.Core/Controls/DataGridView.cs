using Forma.Core.Rendering;
using System.Globalization;

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
    private bool _sortingEnabled = true, _filteringEnabled = true;
    private string _filterText = "", _sortDirection = "ascending";
    private int _sortColumn = -1, _selectedRow = -1;
    public bool SortingEnabled { get => _sortingEnabled; set => SetProperty(ref _sortingEnabled, value); }
    public bool FilteringEnabled { get => _filteringEnabled; set => SetProperty(ref _filteringEnabled, value); }
    public string FilterText { get => _filterText; set { ArgumentNullException.ThrowIfNull(value); SetProperty(ref _filterText, value); } }
    public int SortColumn { get => _sortColumn; set => SetProperty(ref _sortColumn, Math.Clamp(value, -1, _columns.Length - 1)); }
    public string SortDirection { get => _sortDirection; set { if (value is "ascending" or "descending") SetProperty(ref _sortDirection, value); } }
    /// <summary>The index in Rows, independent of the current sort/filter view.</summary>
    public int SelectedRow
    {
        get => _selectedRow;
        set {
            var selected = Math.Clamp(value, -1, _rows.Length - 1);
            if (_selectedRow == selected) return;
            SetProperty(ref _selectedRow, selected);
            RowSelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public event EventHandler? RowSelectionChanged;
    public int[] VisibleRowIndices
    {
        get
        {
            IEnumerable<int> indices = Enumerable.Range(0, _rows.Length);
            if (FilteringEnabled && FilterText.Length > 0)
                indices = indices.Where(index => _rows[index].Take(_columns.Length).Any(cell => cell.Contains(FilterText, StringComparison.OrdinalIgnoreCase)));
            if (SortingEnabled && SortColumn >= 0)
            {
                var comparer = Comparer<string>.Create((left, right) =>
                    double.TryParse(left, NumberStyles.Float, CultureInfo.InvariantCulture, out var a) && double.TryParse(right, NumberStyles.Float, CultureInfo.InvariantCulture, out var b)
                    && double.IsFinite(a) && double.IsFinite(b) ? a.CompareTo(b) : StringComparer.OrdinalIgnoreCase.Compare(left, right));
                string Cell(int index) => _rows[index].ElementAtOrDefault(SortColumn) ?? "";
                indices = SortDirection == "descending" ? indices.OrderByDescending(Cell, comparer) : indices.OrderBy(Cell, comparer);
            }
            return indices.ToArray();
        }
    }
    public string[] Columns
    {
        get => (string[])_columns.Clone();
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (value.Any(column => column is null)) throw new ArgumentException("Grid columns cannot be null.");
            SetProperty(ref _columns, (string[])value.Clone());
            SortColumn = SortColumn;
        }
    }
    public string[][] Rows
    {
        get => _rows.Select(row => (string[])row.Clone()).ToArray();
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (value.Any(row => row is null || row.Any(cell => cell is null))) throw new ArgumentException("Grid rows and cells cannot be null.");
            SetProperty(ref _rows, value.Select(row => (string[])row.Clone()).ToArray());
            SelectedRow = SelectedRow;
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
        {
            // Pad with empty cells: filtering, sorting and the Rows setter all assume non-null cells.
            var length = cells.Length;
            Array.Resize(ref cells, _columns.Length);
            Array.Fill(cells, "", length, cells.Length - length);
        }
        if (cells[column] == value)
            return;
        cells[column] = value;
        _rows[row] = cells;
        OnPropertyChanged(nameof(Rows));
        CellChanged?.Invoke(this, new(row, column, value));
    }

    protected override void WriteRenderState(IDictionary<string, object?> state)
    {
        base.WriteRenderState(state);
        state["columns"] = Columns; state["rows"] = Rows; state["readOnly"] = ReadOnly;
        state["sortingEnabled"] = SortingEnabled; state["filteringEnabled"] = FilteringEnabled;
        state["filterText"] = FilterText; state["sortColumn"] = SortColumn; state["sortDirection"] = SortDirection;
        state["selectedRow"] = SelectedRow; state["visibleRowIndices"] = VisibleRowIndices;
    }

    protected override void OnRuntimeEvent(RuntimeEvent e)
    {
        base.OnRuntimeEvent(e);
        switch (e.Name)
        {
            case "grid-sort" when SortingEnabled && e.TryGetInt("column", out var column) && column >= 0 && column < Columns.Length:
                SortDirection = SortColumn == column && SortDirection == "ascending" ? "descending" : "ascending";
                SortColumn = column;
                break;
            case "grid-filter" when FilteringEnabled && e.TryGetString("text", out var filter):
                FilterText = filter;
                break;
            case "grid-select" when e.TryGetInt("row", out var row) && VisibleRowIndices.Contains(row):
                SelectedRow = row;
                break;
            case "cell" when e.TryGetInt("row", out var cellRow) && e.TryGetInt("column", out var cellColumn) && e.TryGetString("value", out var value):
                SetCell(cellRow, cellColumn, value);
                break;
        }
    }
}
