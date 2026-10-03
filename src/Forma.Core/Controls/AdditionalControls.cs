namespace Forma.Core.Controls;

public class CheckBox : Control
{
    private bool _checked;
    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value)
                return;
            SetProperty(ref _checked, value);
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public event EventHandler? CheckedChanged;
}

public sealed class RadioButton : CheckBox { }

public abstract class ChoiceControl : Control
{
    private string[] _items = ["Item 1", "Item 2", "Item 3"];
    private int _selectedIndex;
    public string[] Items
    {
        get => (string[])_items.Clone();
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            SetProperty(ref _items, (string[])value.Clone());
            if (_selectedIndex >= value.Length)
                SelectedIndex = value.Length - 1;
        }
    }
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            var index = Math.Clamp(value, -1, Math.Max(-1, _items.Length - 1));
            if (_selectedIndex == index)
                return;
            SetProperty(ref _selectedIndex, index);
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public event EventHandler? SelectedIndexChanged;
}

public sealed class ComboBox : ChoiceControl { }

public sealed class ListBox : ChoiceControl { }

public class Image : Control
{
    private string _source = "";
    private string _sizeMode = "contain";
    public string Source
    {
        get => _source;
        set => SetProperty(ref _source, value);
    }
    public string SizeMode
    {
        get => _sizeMode;
        set =>
            SetProperty(ref _sizeMode, value is "contain" or "cover" or "fill" ? value : "contain");
    }
}

public abstract class LayoutContainer : Control
{
    private string _orientation = "horizontal";
    private int _gap = 8;
    private int _columns = 2;
    private string[] _tabs = ["Tab 1", "Tab 2"];
    private int _selectedTab;
    public string Orientation
    {
        get => _orientation;
        set => SetProperty(ref _orientation, value == "vertical" ? value : "horizontal");
    }
    public int Gap
    {
        get => _gap;
        set => SetProperty(ref _gap, Math.Clamp(value, 0, 64));
    }
    public int Columns
    {
        get => _columns;
        set => SetProperty(ref _columns, Math.Clamp(value, 1, 12));
    }
    public string[] Tabs
    {
        get => (string[])_tabs.Clone();
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            SetProperty(ref _tabs, value.Length > 0 ? (string[])value.Clone() : ["Tab 1"]);
            SelectedTab = _selectedTab;
        }
    }
    public int SelectedTab
    {
        get => _selectedTab;
        set => SetProperty(ref _selectedTab, Math.Clamp(value, 0, _tabs.Length - 1));
    }
}

public sealed class GroupBox : LayoutContainer { }

public sealed class SplitContainer : LayoutContainer { }

public sealed class TabControl : LayoutContainer { }

public sealed class FlowLayoutPanel : LayoutContainer { }

public sealed class TableLayoutPanel : LayoutContainer { }

public sealed class CellChangedEventArgs(int row, int column, string value) : EventArgs
{
    public int Row { get; } = row;
    public int Column { get; } = column;
    public string Value { get; } = value;
}

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
