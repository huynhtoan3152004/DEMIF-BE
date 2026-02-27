using Demif.Application.Abstractions.Repositories;
using Demif.Domain.Entities;
using Demif.Infrastructure.Persistence;

namespace Demif.Infrastructure.Repositories
{
    // Kế thừa GenericRepository (đã có sẵn các hàm Add, Update, Delete...) 
    // và thực thi IBlogRepository
    public class BlogRepository : GenericRepository<Blog>, IBlogRepository
    {
        public BlogRepository(ApplicationDbContext context) : base(context)
        {
            // Nếu sau này bạn cần viết các câu query phức tạp dành riêng cho Blog
            // (ví dụ: lấy top 5 bài viết nhiều view nhất), bạn sẽ viết thêm ở đây.
            // Còn các thao tác cơ bản thì GenericRepository đã lo hết rồi!
        }
    }
}