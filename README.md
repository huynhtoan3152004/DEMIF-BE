# DEMIF-BE - Backend API

Backend API cho ứng dụng DEMIF.

## Yêu cầu

- .NET 8.0 SDK
- PostgreSQL 
- Firebase Account

## Setup

### 1. Clone repository

```bash
git clone https://github.com/huynhtoan3152004/DEMIF-BE.git
cd DEMIF-BE
```

### 2. Cấu hình appsettings

Copy file mẫu và điền thông tin cấu hình:

```bash
# Copy file mẫu
copy appsettings.example.json src\Demif.Api\appsettings.json
copy appsettings.example.json src\Demif.Api\appsettings.Development.json
```

Sau đó mở file `src\Demif.Api\appsettings.json` và cập nhật:

- **ConnectionStrings.DefaultConnection**: Connection string PostgreSQL của bạn
- **Jwt.Key**: Secret key cho JWT (ít nhất 32 ký tự)
- **Firebase**: Thông tin Firebase service account (lấy từ Firebase Console)

### 3. Cấu hình Firebase

1. Vào [Firebase Console](https://console.firebase.google.com/)
2. Chọn project của bạn
3. Vào **Project Settings** → **Service Accounts**
4. Click **Generate new private key**
5. Copy nội dung file JSON vào phần Firebase trong `appsettings.json`

### 4. Restore dependencies

```bash
dotnet restore
```

### 5. Run migrations (nếu có)

```bash
dotnet ef database update --project src/Demif.Infrastructure --startup-project src/Demif.Api
```

### 6. Run application

```bash
cd src/Demif.Api
dotnet run
```

API sẽ chạy tại `https://localhost:7xxx`

## ⚠️ Lưu ý bảo mật

**KHÔNG BAO GIỜ** commit các file sau:

- `appsettings.json`
- `appsettings.Development.json`
- `appsettings.Production.json`
- `firebase-credentials.json`
- Bất kỳ file nào chứa thông tin nhạy cảm

Các file này đã được thêm vào `.gitignore`.

## Cấu trúc project

```
src/
├── Demif.Api/              # API Layer
├── Demif.Application/      # Application Layer
├── Demif.Domain/           # Domain Layer
└── Demif.Infrastructure/   # Infrastructure Layer
```

## License

[Thêm license của bạn]
