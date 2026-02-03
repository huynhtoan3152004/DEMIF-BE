namespace Demif.Application.Features.Users.GetUsers;

/// <summary>
/// Request lấy danh sách users với pagination và filter
/// </summary>
public class GetUsersRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
    public string? Status { get; set; }
}
