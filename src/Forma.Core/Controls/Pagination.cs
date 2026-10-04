namespace Forma.Core.Controls;

public sealed class Pagination : Control
{
    private int _totalItems = 100, _pageSize = 10, _page = 1;
    public int TotalItems { get => _totalItems; set { SetProperty(ref _totalItems, Math.Max(0, value)); Page = _page; } }
    public int PageSize { get => _pageSize; set { SetProperty(ref _pageSize, Math.Clamp(value, 1, 10000)); Page = _page; } }
    public int PageCount => Math.Max(1, (int)Math.Ceiling((double)_totalItems / _pageSize));
    public int Page { get => _page; set { var next = Math.Clamp(value, 1, PageCount); if (next == _page) return; SetProperty(ref _page, next); PageChanged?.Invoke(this, EventArgs.Empty); } }
    public event EventHandler? PageChanged;
}
