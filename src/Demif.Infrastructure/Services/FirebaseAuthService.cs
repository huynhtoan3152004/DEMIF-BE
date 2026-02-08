using Demif.Application.Abstractions.Services;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

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

        try
        {
            // Khởi tạo Firebase App nếu chưa có
            if (FirebaseApp.DefaultInstance == null)
            {
                var firebaseConfig = configuration.GetSection("Firebase").Get<Dictionary<string, string>>();
                
                if (firebaseConfig == null || !firebaseConfig.ContainsKey("private_key"))
                {
                    throw new InvalidOperationException("Firebase configuration is missing or incomplete");
                }

                // ✅ FIX: Xử lý private_key để có newlines thật
                var privateKey = firebaseConfig["private_key"];
                
                // Thay thế tất cả các dạng escape của newline
                privateKey = privateKey
                    .Replace("\\\\n", "\n")  // \\n → \n
                    .Replace("\\n", "\n");    // \n → newline thật
                
                // Build JSON manually để tránh escape lại
                var firebaseJson = BuildFirebaseJson(firebaseConfig, privateKey);

                // Log để debug (không log private_key)
                _logger.LogInformation("Initializing Firebase for project: {ProjectId}", 
                    firebaseConfig["project_id"]);

                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromJson(firebaseJson)
                });

                _logger.LogInformation("✅ Firebase Admin SDK initialized successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to initialize Firebase: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Build Firebase JSON manually để tránh JsonSerializer escape newlines
    /// </summary>
    private static string BuildFirebaseJson(Dictionary<string, string> config, string privateKey)
    {
        // Escape các ký tự đặc biệt NGOẠI TRỪ newline
        string EscapeJsonValue(string value)
        {
            return value
                .Replace("\\", "\\\\")  // \ → \\
                .Replace("\"", "\\\"")  // " → \"
                .Replace("\r", "")      // Remove carriage return
                .Replace("\t", "\\t");  // Tab → \t
        }

        var jsonBuilder = new System.Text.StringBuilder();
        jsonBuilder.AppendLine("{");
        
        bool isFirst = true;
        foreach (var kvp in config)
        {
            if (!isFirst) jsonBuilder.AppendLine(",");
            isFirst = false;

            var value = kvp.Key == "private_key" ? privateKey : kvp.Value;
            
            // Escape value nhưng GIỮ NGUYÊN \n trong private_key
            if (kvp.Key == "private_key")
            {
                // Đảm bảo private_key có \n thật, rồi escape cho JSON
                value = value.Replace("\"", "\\\"");
                jsonBuilder.Append($"  \"{kvp.Key}\": \"{value}\"");
            }
            else
            {
                value = EscapeJsonValue(value);
                jsonBuilder.Append($"  \"{kvp.Key}\": \"{value}\"");
            }
        }
        
        jsonBuilder.AppendLine();
        jsonBuilder.Append("}");
        
        return jsonBuilder.ToString();
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
