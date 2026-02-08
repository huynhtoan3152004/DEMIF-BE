# 🚀 Hướng Dẫn Deploy Demif-BE lên Coolify

## 📋 Tổng Quan

Tài liệu này hướng dẫn cách deploy ứng dụng Demif-BE .NET 8 lên **Coolify** - một nền tảng self-hosted PaaS.

---

## 🔧 Các File Đã Tạo

| File | Mô tả |
|------|-------|
| `Dockerfile` | Multi-stage build cho .NET 8 API |
| `docker-compose.yml` | Cấu hình services (API + PostgreSQL) |
| `.env.example` | Template biến môi trường |
| `.dockerignore` | Loại trừ files không cần thiết khỏi Docker build |

---

## 📝 Cách Setup Trên Coolify

### Bước 1: Chuẩn Bị Repository

1. Push code lên GitHub/GitLab
2. Đảm bảo `Dockerfile` nằm ở root của repository

### Bước 2: Tạo Project Trên Coolify

1. Đăng nhập vào Coolify Dashboard
2. Tạo **New Project** → đặt tên (vd: `Demif`)
3. Chọn **Environment** (production)

### Bước 3: Deploy API (Chọn 1 trong 2 cách)

#### Cách A: Dockerfile-based (Khuyến nghị)

1. Click **New Resource** → **Docker Based** → **Dockerfile**
2. Connect repository GitHub của bạn
3. Cấu hình:
   - **Branch**: `main` hoặc `master`
   - **Dockerfile Location**: `Dockerfile`
   - **Port**: `8080`

#### Cách B: Docker Compose

1. Click **New Resource** → **Docker Based** → **Docker Compose Empty**
2. Paste nội dung từ `docker-compose.yml`
3. Cấu hình domains và ports

### Bước 4: Tạo Database PostgreSQL

1. **New Resource** → **Databases** → **PostgreSQL**
2. Cấu hình:
   - **Database Name**: `demif_db`
   - **Username**: `demif`
   - **Password**: Tạo password mạnh

### Bước 5: Cấu Hình Environment Variables

Trong Coolify, vào **Settings** của resource API và thêm:

```bash
# Database (sử dụng Internal URL từ Coolify)
ConnectionStrings__DefaultConnection=Host=<postgres-hostname>;Port=5432;Database=demif_db;Username=demif;Password=<your-password>;

# JWT
Jwt__Key=YourSuperSecretKeyAtLeast32CharactersLong!
Jwt__Issuer=Demif.Api
Jwt__Audience=Demif.Client
Jwt__ExpirationMinutes=60

# ASP.NET Core
ASPNETCORE_ENVIRONMENT=Production
```

> 💡 **Tip**: Để lấy Connection String, xem Internal URL trong settings của PostgreSQL resource trên Coolify.

### Bước 6: Cấu Hình Domain

1. Trong **Domains** tab, thêm domain của bạn
2. Bật **HTTPS** (Coolify sẽ tự động tạo SSL cert với Let's Encrypt)

### Bước 7: Deploy

1. Click **Deploy** button
2. Theo dõi logs để đảm bảo build thành công

---

## 🩺 Health Check

API có endpoint health check tại `/health`. Coolify sẽ tự sử dụng endpoint này để kiểm tra trạng thái.

> ⚠️ **Lưu ý**: Bạn cần thêm health check endpoint vào API nếu chưa có:

```csharp
// Trong Program.cs
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
```

---

## 🔄 CI/CD Auto Deploy

1. Trong Coolify resource settings → **Webhooks**
2. Copy webhook URL
3. Thêm vào GitHub repository:
   - **Settings** → **Webhooks** → **Add webhook**
   - Paste URL và chọn **push** events

---

## 📊 Monitoring

Coolify cung cấp:
- **Logs**: Xem real-time logs của container
- **Resource Usage**: CPU, Memory, Network
- **Deployment History**: Rollback nếu cần

---

## 🛠️ Troubleshooting

| Vấn đề | Giải pháp |
|--------|-----------|
| Build failed | Kiểm tra logs, đảm bảo Dockerfile syntax đúng |
| Cannot connect to database | Kiểm tra Connection String và network |
| Health check failed | Đảm bảo endpoint `/health` trả về status 200 |
| Port không hoạt động | Verify port `8080` được expose đúng |

---

## 📁 Cấu Trúc Project

```
Demif-BE/
├── Dockerfile              # Docker build configuration
├── docker-compose.yml      # Multi-service orchestration
├── .dockerignore           # Excluded files from build
├── .env.example            # Environment variables template
└── src/
    ├── Demif.Api/          # API Layer
    ├── Demif.Application/  # Business Logic
    ├── Demif.Domain/       # Domain Entities
    └── Demif.Infrastructure/ # Data Access
```
