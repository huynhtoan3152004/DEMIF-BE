namespace Demif.Application.Features.Blogs.GetBlogs;

public class PagedBlogResponse
{
    public List<BlogDto> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}
