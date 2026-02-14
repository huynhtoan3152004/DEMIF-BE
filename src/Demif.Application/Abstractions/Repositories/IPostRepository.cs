using Demif.Domain.Entities;

namespace Demif.Application.Abstractions.Repositories;

public interface IPostRepository : IGenericRepository<Post>
{
    Task<IEnumerable<Post>> GetPostsWithDetailsAsync(CancellationToken cancellationToken = default);
    Task<Post?> GetPostWithDetailsAsync(Guid postId, CancellationToken cancellationToken = default);
    Task<bool> ToggleLikeAsync(Guid postId, Guid userId);
    Task AddCommentAsync(Comment comment);
    Task DeleteCommentAsync(Guid commentId, Guid userId);
}