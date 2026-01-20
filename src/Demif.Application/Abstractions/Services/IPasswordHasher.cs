namespace Demif.Application.Abstractions.Services;

/// <summary>
/// Password Hasher interface
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}
