namespace Demif.Application.Features.Auth.FirebaseLogin;

/// <summary>
/// Request để đăng nhập bằng Firebase ID Token
/// </summary>
public record FirebaseLoginRequest(
    /// <summary>
    /// Firebase ID Token từ client (sau khi user đăng nhập Google/Email trên client)
    /// </summary>
    string IdToken
);
