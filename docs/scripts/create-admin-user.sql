-- =====================================================
-- SCRIPT: Tạo Admin User cho DEMIF
-- Database: PostgreSQL
-- =====================================================

-- Bước 1: Tạo User với password đã hash (BCrypt)
-- Password gốc: 123
-- BCrypt hash được tạo với cost factor 11

INSERT INTO "Users" (
    "Id",
    "Email",
    "PasswordHash",
    "Username",
    "AvatarUrl",
    "Status",
    "Country",
    "NativeLanguage",
    "TargetLanguage",
    "CurrentLevel",
    "DailyGoalMinutes",
    "FirebaseUid",
    "AuthProvider",
    "Settings",
    "CreatedAt",
    "UpdatedAt",
    "LastLoginAt"
)
VALUES (
    'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',  -- Id (fixed để dễ reference)
    'toanadmin@demif.com',                    -- Email
    '$2a$11$rKN5dEhRl3/AQyQlJcNzYeYHYtWlGQJPnM0VB8c8b1b0bRqnZqK/O',  -- BCrypt hash của "123"
    'toanadmin',                              -- Username
    NULL,                                     -- AvatarUrl
    0,                                        -- Status (0 = Active)
    'Vietnam',                                -- Country
    'Vietnamese',                             -- NativeLanguage
    'English',                                -- TargetLanguage
    0,                                        -- CurrentLevel (0 = Beginner)
    30,                                       -- DailyGoalMinutes
    NULL,                                     -- FirebaseUid (không dùng Firebase)
    'email',                                  -- AuthProvider
    NULL,                                     -- Settings
    NOW(),                                    -- CreatedAt
    NULL,                                     -- UpdatedAt
    NULL                                      -- LastLoginAt
);

-- Bước 2: Gán Role "Admin" cho user
-- Role Admin có Id: 11111111-1111-1111-1111-111111111111 (từ seed data)

INSERT INTO "UserRoles" (
    "Id",
    "UserId",
    "RoleId",
    "AssignedAt",
    "AssignedBy",
    "ExpiresAt",
    "CreatedAt",
    "UpdatedAt"
)
VALUES (
    gen_random_uuid(),                               -- Id
    'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',          -- UserId (user vừa tạo)
    '11111111-1111-1111-1111-111111111111',          -- RoleId (Admin)
    NOW(),                                           -- AssignedAt
    NULL,                                            -- AssignedBy (system)
    NULL,                                            -- ExpiresAt (không hết hạn)
    NOW(),                                           -- CreatedAt
    NULL                                             -- UpdatedAt
);

-- Bước 3: Kiểm tra user đã tạo
SELECT u."Id", u."Username", u."Email", r."Name" as "Role"
FROM "Users" u
JOIN "UserRoles" ur ON u."Id" = ur."UserId"
JOIN "Roles" r ON ur."RoleId" = r."Id"
WHERE u."Username" = 'toanadmin';

-- =====================================================
-- KẾT QUẢ:
-- Username: toanadmin
-- Email: toanadmin@demif.com
-- Password: 123
-- Role: Admin
-- =====================================================
