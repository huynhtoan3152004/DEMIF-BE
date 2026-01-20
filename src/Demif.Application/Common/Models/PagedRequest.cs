namespace Demif.Application.Common.Models;

/// <summary>
/// Request phân trang cơ bản
/// </summary>
public class PagedRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
}
