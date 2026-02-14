using Demif.Application.Abstractions.Repositories;
using Demif.Domain.Entities;
using Demif.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Demif.Infrastructure.Repositories;

public class PostRepository : GenericRepository<Post>, IPostRepository
{
    public PostRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<Post>> GetPostsWithDetailsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Author)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Post?> GetPostWithDetailsAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Author)
            .Include(p => p.Comments).ThenInclude(c => c.User)
            .Include(p => p.Likes)
            .FirstOrDefaultAsync(p => p.Id == postId, cancellationToken);
    }

    public async Task<bool> ToggleLikeAsync(Guid postId, Guid userId)
    {
        var existingLike = await _context.PostLikes
            .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

        if (existingLike != null)
        {
            _context.PostLikes.Remove(existingLike);
            await _context.SaveChangesAsync();
            return false; 
        }

        _context.PostLikes.Add(new PostLike { PostId = postId, UserId = userId });
        await _context.SaveChangesAsync();
        return true; 
    }

    public async Task AddCommentAsync(Comment comment)
    {
        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteCommentAsync(Guid commentId, Guid userId)
    {
        var comment = await _context.Comments.FindAsync(commentId);
        if (comment != null && comment.UserId == userId)
        {
            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
        }
    }
}