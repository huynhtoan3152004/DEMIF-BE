using Demif.Application.Abstractions.Repositories;
using Demif.Application.Abstractions.Services;
using Demif.Application.Common.Models;
using Demif.Domain.Entities;

namespace Demif.Application.Features.Blog;

public class BlogService
{
    private readonly IPostRepository _postRepository;
    private readonly ICurrentUserService _currentUser;

    public BlogService(IPostRepository postRepository, ICurrentUserService currentUser)
    {
        _postRepository = postRepository;
        _currentUser = currentUser;
    }

    public async Task<IEnumerable<Post>> GetListAsync() => await _postRepository.GetPostsWithDetailsAsync();

    public async Task<Result> CreateAsync(string title, string content)
    {
        var userId = _currentUser.UserId;
        if (userId == null) return Result.Failure(Error.Unauthorized());

        var post = new Post { Title = title, Content = content, AuthorId = userId.Value };
        await _postRepository.AddAsync(post);
        return Result.Success();
    }

    public async Task<Result> UpdateAsync(Guid id, string title, string content)
    {
        var post = await _postRepository.GetByIdAsync(id);
        if (post == null) return Result.Failure(Error.NotFound("Post", id));
        if (post.AuthorId != _currentUser.UserId) return Result.Failure(Error.Forbidden());

        post.Title = title;
        post.Content = content;
        post.UpdatedAt = DateTime.UtcNow;
        await _postRepository.UpdateAsync(post);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id)
    {
        var post = await _postRepository.GetByIdAsync(id);
        if (post == null) return Result.Failure(Error.NotFound("Post", id));
        if (post.AuthorId != _currentUser.UserId) return Result.Failure(Error.Forbidden());

        await _postRepository.DeleteAsync(post);
        return Result.Success();
    }

    public async Task<Result<bool>> ToggleLikeAsync(Guid postId)
    {
        var userId = _currentUser.UserId;
        if (userId == null) return Result.Failure<bool>(Error.Unauthorized());
        return await _postRepository.ToggleLikeAsync(postId, userId.Value);
    }

    public async Task<Result> AddCommentAsync(Guid postId, string content)
    {
        var userId = _currentUser.UserId;
        if (userId == null) return Result.Failure(Error.Unauthorized());

        await _postRepository.AddCommentAsync(new Comment { PostId = postId, UserId = userId.Value, Content = content });
        return Result.Success();
    }
}