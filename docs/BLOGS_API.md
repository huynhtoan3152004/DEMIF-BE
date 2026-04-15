# DEMIF - Blog API Reference

> Base URL: `/api`
> Auth: JWT Bearer Token cho admin CRUD, public cho đọc bài viết
> Updated: 2026-04-16

Tài liệu này mô tả luồng Blog mới sau khi nâng cấp theo hướng CMS nhẹ: có slug SEO, phân trang, lọc theo category/tag, metadata tác giả, thời gian đọc, tăng view count khi mở bài, và soft delete cho admin.

---

## 1. Overview

### Public
- `GET /api/blogs`
- `GET /api/blogs/{id}`
- `GET /api/blogs/slug/{slug}`

### Admin
- `GET /api/admin/blogs`
- `POST /api/admin/blogs`
- `PUT /api/admin/blogs/{id}`
- `DELETE /api/admin/blogs/{id}`

### Frontend rules
- Dùng `slug` cho URL public để thân thiện SEO.
- Dùng `GET /api/blogs?page=1&pageSize=12...` để render danh sách public.
- Dùng `GET /api/admin/blogs` để render trang quản trị, vì endpoint này trả cả bài đã archive.
- Khi user mở bài qua detail API, backend tự tăng `viewCount`.
- Xóa bài ở admin là soft delete: backend chuyển `status` sang `archived` và set `isDeleted = true`.

---

## 2. Data Models

### 2.1 BlogDto

Response item cho cả list và detail.

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "How to improve pronunciation fast",
  "slug": "how-to-improve-pronunciation-fast",
  "category": "pronunciation",
  "content": "Full blog content here...",
  "summary": "Short intro paragraph for list view",
  "thumbnailUrl": "https://cdn.example.com/blogs/cover.jpg",
  "tags": "pronunciation, speaking, tips",
  "status": "published",
  "publishedAt": "2026-04-16T08:30:00Z",
  "readingTimeMinutes": 4,
  "isFeatured": true,
  "viewCount": 128,
  "authorId": "8d2d5e42-2c3b-4d4f-8f8c-95f9f7a3f4d2",
  "authorName": "admin",
  "authorAvatarUrl": "https://cdn.example.com/avatars/admin.png",
  "createdAt": "2026-04-16T08:00:00Z",
  "updatedAt": "2026-04-16T08:30:00Z"
}
```

### 2.2 PagedBlogResponse

```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "title": "How to improve pronunciation fast",
      "slug": "how-to-improve-pronunciation-fast",
      "category": "pronunciation",
      "content": "Full blog content here...",
      "summary": "Short intro paragraph for list view",
      "thumbnailUrl": "https://cdn.example.com/blogs/cover.jpg",
      "tags": "pronunciation, speaking, tips",
      "status": "published",
      "publishedAt": "2026-04-16T08:30:00Z",
      "readingTimeMinutes": 4,
      "isFeatured": true,
      "viewCount": 128,
      "authorId": "8d2d5e42-2c3b-4d4f-8f8c-95f9f7a3f4d2",
      "authorName": "admin",
      "authorAvatarUrl": "https://cdn.example.com/avatars/admin.png",
      "createdAt": "2026-04-16T08:00:00Z",
      "updatedAt": "2026-04-16T08:30:00Z"
    }
  ],
  "page": 1,
  "pageSize": 12,
  "totalCount": 1,
  "totalPages": 1
}
```

---

## 3. Public APIs

### 3.1 List published blogs

`GET /api/blogs`

Query params:
- `page` default `1`
- `pageSize` default `12`
- `search` keyword tìm trong title, summary, content
- `category` lọc theo category
- `tag` lọc theo tags
- `status` mặc định backend ép về `published`
- `sortBy` mặc định `publishedAt`
- `sortDirection` mặc định `desc`

Example:

`GET /api/blogs?page=1&pageSize=12&search=pronunciation&category=tips&tag=speaking&sortBy=publishedAt&sortDirection=desc`

Response 200:

```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "title": "How to improve pronunciation fast",
      "slug": "how-to-improve-pronunciation-fast",
      "category": "pronunciation",
      "content": "Full blog content here...",
      "summary": "Short intro paragraph for list view",
      "thumbnailUrl": "https://cdn.example.com/blogs/cover.jpg",
      "tags": "pronunciation, speaking, tips",
      "status": "published",
      "publishedAt": "2026-04-16T08:30:00Z",
      "readingTimeMinutes": 4,
      "isFeatured": true,
      "viewCount": 128,
      "authorId": "8d2d5e42-2c3b-4d4f-8f8c-95f9f7a3f4d2",
      "authorName": "admin",
      "authorAvatarUrl": "https://cdn.example.com/avatars/admin.png",
      "createdAt": "2026-04-16T08:00:00Z",
      "updatedAt": "2026-04-16T08:30:00Z"
    }
  ],
  "page": 1,
  "pageSize": 12,
  "totalCount": 1,
  "totalPages": 1
}
```

### 3.2 Get blog by ID

`GET /api/blogs/{id}`

Response 200: trả về 1 object BlogDto và tự tăng `viewCount`.

Response 404:

```json
{
  "message": "Không tìm thấy bài viết"
}
```

### 3.3 Get blog by slug

`GET /api/blogs/slug/{slug}`

Example:

`GET /api/blogs/slug/how-to-improve-pronunciation-fast`

Response 200: trả về BlogDto và tự tăng `viewCount`.

Response 404:

```json
{
  "message": "Không tìm thấy bài viết"
}
```

---

## 4. Admin APIs

### 4.1 List all blogs for CMS

`GET /api/admin/blogs`

Auth: `Bearer Token` với policy `RequireAdmin`

Query params giống public list, nhưng backend trả cả bài đã archive vì `includeDeleted = true`.

Response 200: cùng shape với `PagedBlogResponse`.

Frontend note:
- Dùng endpoint này cho bảng quản trị.
- Có thể show trạng thái `published`, `draft`, `archived`.
- Có thể filter theo `status`, `category`, `tag`, `search`.

### 4.2 Create blog

`POST /api/admin/blogs`

Auth: `Bearer Token` với policy `RequireAdmin`

Content-Type: `multipart/form-data`

Form fields:
- `title` required
- `content` required
- `slug` optional
- `category` optional
- `summary` optional
- `thumbnailFile` optional file
- `tags` optional string
- `isFeatured` optional boolean
- `status` default `published`

JSON-equivalent request for FE mapping:

```json
{
  "title": "How to improve pronunciation fast",
  "content": "Full blog content here...",
  "slug": "how-to-improve-pronunciation-fast",
  "category": "pronunciation",
  "summary": "Short intro paragraph for list view",
  "tags": "pronunciation, speaking, tips",
  "isFeatured": true,
  "status": "published"
}
```

Success response 201:

```json
{
  "message": "Tạo bài viết thành công",
  "blogId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

Important:
- Nếu `slug` không truyền, backend tự tạo từ `title`.
- Nếu slug bị trùng, backend tự thêm hậu tố `-2`, `-3`, ...

### 4.3 Update blog

`PUT /api/admin/blogs/{id}`

Auth: `Bearer Token` với policy `RequireAdmin`

Content-Type: `multipart/form-data`

Form fields:
- `title` required
- `content` required
- `slug` optional
- `category` optional
- `summary` optional
- `thumbnailFile` optional file
- `tags` optional string
- `isFeatured` optional boolean
- `status` default `published`

JSON-equivalent request:

```json
{
  "title": "How to improve pronunciation fast",
  "content": "Updated blog content...",
  "slug": "pronunciation-fast-guide",
  "category": "pronunciation",
  "summary": "Updated summary",
  "tags": "pronunciation, speaking, tips",
  "isFeatured": false,
  "status": "published"
}
```

Success response 200:

```json
{
  "message": "Cập nhật bài viết thành công"
}
```

### 4.4 Delete blog

`DELETE /api/admin/blogs/{id}`

Auth: `Bearer Token` với policy `RequireAdmin`

Behavior:
- Không xóa cứng.
- Backend set `status = archived`, `isDeleted = true`, `deletedAt = now`.

Success response 200:

```json
{
  "message": "Xóa bài viết thành công"
}
```

---

## 5. FE Integration Notes

### 5.1 List page
- Dùng `PagedBlogResponse.items` để render card list.
- Dùng `page`, `pageSize`, `totalCount`, `totalPages` để pagination UI.
- Hiển thị `summary`, `thumbnailUrl`, `readingTimeMinutes`, `authorName`, `publishedAt`.

### 5.2 Detail page
- Ưu tiên route slug: `/blogs/{slug}`.
- Khi mở detail, gọi `GET /api/blogs/slug/{slug}`.
- Có thể fallback bằng `GET /api/blogs/{id}` nếu đang giữ id cũ.

### 5.3 Admin CMS
- Dùng `GET /api/admin/blogs` cho bảng danh sách.
- Hiển thị thêm `status`, `isFeatured`, `viewCount`, `updatedAt`.
- Với bài archived, nên cho phép restore nếu backend về sau mở thêm API restore.

### 5.4 Search and filters
- `search` tìm toàn văn ở title, summary, content.
- `category` và `tag` là filter string đơn giản.
- `sortBy` hỗ trợ: `publishedAt`, `createdAt`, `title`, `views`.
- `sortDirection` hỗ trợ: `asc`, `desc`.

---

## 6. Common Errors

### 400 Bad Request
```json
{
  "message": "Tiêu đề không được để trống"
}
```

### 404 Not Found
```json
{
  "message": "Không tìm thấy bài viết"
}
```

### 403 Forbidden
```json
{
  "message": "Forbidden"
}
```

### 401 Unauthorized
```json
{
  "message": "Unauthorized"
}
```

---

## 7. Suggested FE payload mapping

### Create / Update form
```json
{
  "title": "string",
  "content": "string",
  "slug": "string",
  "category": "string",
  "summary": "string",
  "thumbnailFile": "file",
  "tags": "string",
  "isFeatured": false,
  "status": "published"
}
```

### List query
```json
{
  "page": 1,
  "pageSize": 12,
  "search": "pronunciation",
  "category": "tips",
  "tag": "speaking",
  "status": "published",
  "sortBy": "publishedAt",
  "sortDirection": "desc"
}
```

---

## 8. Notes for future expansion

- Comments table
- Likes/bookmarks table
- Draft preview and scheduled publish
- Pinned posts order
- Normalized tags/categories
- Rich text sanitation for HTML/Markdown content
