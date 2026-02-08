using Demif.Application.Abstractions.Services;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Demif.Infrastructure.Services;

/// <summary>
/// Firebase Authentication Service implementation
/// Sử dụng Firebase Admin SDK để verify tokens
/// </summary>
public class FirebaseAuthService : IFirebaseAuthService
{
    private readonly ILogger<FirebaseAuthService> _logger;

    public FirebaseAuthService(IConfiguration configuration, ILogger<FirebaseAuthService> logger)
    {
        _logger = logger;

        // Khởi tạo Firebase App nếu chưa có
        if (FirebaseApp.DefaultInstance == null)
        {
            var firebaseConfig = configuration.GetSection("Firebase").Get<Dictionary<string, string>>();
            
            // Tạo JSON credentials từ config
            var firebaseJson = System.Text.Json.JsonSerializer.Serialize(firebaseConfig);

            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromJson(firebaseJson)
            });

            _logger.LogInformation("Firebase Admin SDK initialized successfully for project: {ProjectId}", 
                firebaseConfig["project_id"]);
        }
    }

    /// <inheritdoc />
    public async Task<FirebaseToken> VerifyIdTokenAsync(string idToken)
    {
        try
        {
            var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);
            _logger.LogDebug("Token verified for user: {Uid}", decodedToken.Uid);
            return decodedToken;
        }
        catch (FirebaseAuthException ex)
        {
            _logger.LogWarning("Failed to verify Firebase token: {Message}", ex.Message);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<UserRecord> GetUserByUidAsync(string uid)
    {
        try
        {
            return await FirebaseAuth.DefaultInstance.GetUserAsync(uid);
        }
        catch (FirebaseAuthException ex)
        {
            _logger.LogWarning("Failed to get user by UID {Uid}: {Message}", uid, ex.Message);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<UserRecord> GetUserByEmailAsync(string email)
    {
        try
        {
            return await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(email);
        }
        catch (FirebaseAuthException ex)
        {
            _logger.LogWarning("Failed to get user by email {Email}: {Message}", email, ex.Message);
            throw;
        }
    }
}
