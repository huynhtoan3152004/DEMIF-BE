using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Services;
using Demif.Application.Common.Models;
using Demif.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Demif.Application.Features.Auth.FirebaseLogin;

/// <summary>
/// Service xử lý đăng nhập bằng Firebase
/// Flow:
/// 1. Verify Firebase ID token
/// 2. Tìm user trong DB theo FirebaseUid
/// 3. Nếu chưa có -> tạo mới user + gán role mặc định
/// 4. Generate JWT access token
/// </summary>
public class FirebaseLoginService
{
    private readonly IFirebaseAuthService _firebaseAuth;
    private readonly IApplicationDbContext _dbContext;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<FirebaseLoginService> _logger;

    public FirebaseLoginService(
        IFirebaseAuthService firebaseAuth,
        IApplicationDbContext dbContext,
        IJwtTokenService jwtTokenService,
        ILogger<FirebaseLoginService> logger)
    {
        _firebaseAuth = firebaseAuth;
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<Result<FirebaseLoginResponse>> ExecuteAsync(
        FirebaseLoginRequest request, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Verify Firebase token
            var firebaseToken = await _firebaseAuth.VerifyIdTokenAsync(request.IdToken);
            var firebaseUid = firebaseToken.Uid;
            var email = firebaseToken.Claims.TryGetValue("email", out var emailClaim) 
                ? emailClaim?.ToString() ?? "" 
                : "";
            var name = firebaseToken.Claims.TryGetValue("name", out var nameClaim) 
                ? nameClaim?.ToString() 
                : null;
            var picture = firebaseToken.Claims.TryGetValue("picture", out var pictureClaim) 
                ? pictureClaim?.ToString() 
                : null;

            _logger.LogInformation("Firebase token verified for UID: {Uid}, Email: {Email}", firebaseUid, email);

            // 2. Tìm user trong DB
            var user = await _dbContext.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.FirebaseUid == firebaseUid, cancellationToken);

            bool isNewUser = false;

            if (user == null)
            {
                // 3. Tạo user mới
                isNewUser = true;
                user = new User
                {
                    Email = email,
                    Username = name ?? email.Split('@')[0],
                    FirebaseUid = firebaseUid,
                    AuthProvider = firebaseToken.Claims.TryGetValue("firebase", out var firebase) 
                        ? GetAuthProvider(firebase) 
                        : "firebase",
                    AvatarUrl = picture,
                    LastLoginAt = DateTime.UtcNow
                };

                _dbContext.Users.Add(user);

                // Gán role mặc định (User)
                var defaultRole = await _dbContext.Roles
                    .FirstOrDefaultAsync(r => r.IsDefault && r.IsActive, cancellationToken);

                if (defaultRole != null)
                {
                    var userRole = new UserRole
                    {
                        UserId = user.Id,
                        RoleId = defaultRole.Id,
                        AssignedAt = DateTime.UtcNow
                    };
                    _dbContext.UserRoles.Add(userRole);
                }

                await _dbContext.SaveChangesAsync(cancellationToken);

                // Reload với roles
                user = await _dbContext.Users
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .FirstAsync(u => u.Id == user.Id, cancellationToken);

                _logger.LogInformation("New user created: {UserId}, Email: {Email}", user.Id, email);
            }
            else
            {
                // Update last login
                user.LastLoginAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            // 4. Get user roles
            var roles = user.UserRoles
                .Where(ur => ur.Role.IsActive && (ur.ExpiresAt == null || ur.ExpiresAt > DateTime.UtcNow))
                .Select(ur => ur.Role.Name)
                .ToList();

            // 5. Generate JWT token
            var primaryRole = roles.FirstOrDefault() ?? "User";
            var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email, primaryRole);

            return Result.Success<FirebaseLoginResponse>(new FirebaseLoginResponse(
                UserId: user.Id,
                Email: user.Email,
                Username: user.Username,
                AccessToken: accessToken,
                Roles: roles,
                IsNewUser: isNewUser
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Firebase login failed");
            return Result.Failure<FirebaseLoginResponse>(new Error("Auth.FirebaseLoginFailed", ex.Message));
        }
    }

    private static string GetAuthProvider(object? firebaseClaim)
    {
        if (firebaseClaim == null) return "firebase";
        
        var json = firebaseClaim.ToString();
        if (json?.Contains("google.com") == true) return "google";
        if (json?.Contains("facebook.com") == true) return "facebook";
        if (json?.Contains("password") == true) return "email";
        
        return "firebase";
    }
}
