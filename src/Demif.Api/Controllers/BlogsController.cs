using Demif.Application.Features.Blogs.CreateBlog;
using Demif.Application.Features.Blogs.DeleteBlog;
using Demif.Application.Features.Blogs.GetBlogById;
using Demif.Application.Features.Blogs.GetBlogs;
using Demif.Application.Features.Blogs.UpdateBlog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Demif.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlogsController : ControllerBase
    {
        private readonly ICreateBlogService _createBlogService;
        private readonly IGetBlogsService _getBlogsService;
        private readonly IGetBlogByIdService _getBlogByIdService;
        private readonly IUpdateBlogService _updateBlogService;
        private readonly IDeleteBlogService _deleteBlogService;

        public BlogsController(
            ICreateBlogService createBlogService,
            IGetBlogsService getBlogsService,
            IGetBlogByIdService getBlogByIdService,
            IUpdateBlogService updateBlogService,
            IDeleteBlogService deleteBlogService)
        {
            _createBlogService = createBlogService;
            _getBlogsService = getBlogsService;
            _getBlogByIdService = getBlogByIdService;
            _updateBlogService = updateBlogService;
            _deleteBlogService = deleteBlogService;
        }


        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetBlogs()
        {
            var blogs = await _getBlogsService.ExecuteAsync();
            return Ok(blogs);
        }

        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetBlogById(Guid id)
        {
            var blog = await _getBlogByIdService.ExecuteAsync(id);
            if (blog == null) return NotFound(new { message = "Không tìm thấy bài viết" });

            return Ok(blog);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateBlog([FromBody] CreateBlogRequest request)
        {
            try
            {
                var blogId = await _createBlogService.ExecuteAsync(request);
                return CreatedAtAction(nameof(GetBlogById), new { id = blogId }, new { message = "Tạo bài viết thành công", blogId = blogId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateBlog(Guid id, [FromBody] UpdateBlogRequest request)
        {
            var isSuccess = await _updateBlogService.ExecuteAsync(id, request);
            if (!isSuccess) return NotFound(new { message = "Không tìm thấy bài viết để cập nhật" });

            return Ok(new { message = "Cập nhật bài viết thành công" });
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteBlog(Guid id)
        {
            var isSuccess = await _deleteBlogService.ExecuteAsync(id);
            if (!isSuccess) return NotFound(new { message = "Không tìm thấy bài viết để xóa" });

            return Ok(new { message = "Xóa bài viết thành công" });
        }
    }
}