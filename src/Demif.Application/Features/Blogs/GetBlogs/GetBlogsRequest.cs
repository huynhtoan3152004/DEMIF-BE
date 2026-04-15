namespace Demif.Application.Features.Blogs.GetBlogs;

public class GetBlogsRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public string? Search { get; set; }
    public string? Category { get; set; }
    public string? Tag { get; set; }
    public string? Status { get; set; }
    public string SortBy { get; set; } = "publishedAt";
    public string SortDirection { get; set; } = "desc";
    public bool IncludeDeleted { get; set; }
}
