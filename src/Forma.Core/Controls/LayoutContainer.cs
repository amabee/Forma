namespace Forma.Core.Controls;

public abstract class LayoutContainer : Control
{
    private string _orientation = "horizontal";
    private int _gap = 8;
    private int _columns = 2;
    private int _rowCount = 2;
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
    public int RowCount { get => _rowCount; set => SetProperty(ref _rowCount, Math.Clamp(value, 1, 100)); }
    public int SelectedTab
    {
        get => _selectedTab;
        set => SetProperty(ref _selectedTab, Math.Clamp(value, 0, _tabs.Length - 1));
    }
}
