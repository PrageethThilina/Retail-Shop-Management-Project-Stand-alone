namespace RetailShop.Core.Common;

public class PagedRequest
{
    private int _pageNumber = 1;
    private int _pageSize = 10;

    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => 10,
            > 200 => 200,
            _ => value
        };
    }

    public string? Search { get; set; }
    public string? SortBy { get; set; }
    public bool IsAscending { get; set; } = true;

    public int Offset => (PageNumber - 1) * PageSize;
}
