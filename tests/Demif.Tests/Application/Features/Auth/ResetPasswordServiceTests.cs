using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Abstractions.Services;
using Demif.Application.Features.Auth.ForgotPassword;
using Demif.Domain.Entities;
using Demif.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Demif.Tests.Application.Features.Auth;

public class ResetPasswordServiceTests
{
    private readonly DbContextOptions<TestDbContext> _dbContextOptions;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<IRefreshTokenRepository> _mockRefreshTokenRepository;

    public ResetPasswordServiceTests()
    {
        _dbContextOptions = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockRefreshTokenRepository = new Mock<IRefreshTokenRepository>();
    }

    private TestDbContext CreateDbContext() => new TestDbContext(_dbContextOptions);

    [Fact]
    public async Task ExecuteAsync_WhenTokenIsInvalidOrExpired_ShouldReturnFailure()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = new ResetPasswordService(context, _mockPasswordHasher.Object, _mockRefreshTokenRepository.Object);
        var request = new ResetPasswordRequest { Token = "invalid_token", NewPassword = "NewPassword123!" };

        // Act
        var result = await service.ExecuteAsync(request);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Link đổi mật khẩu đã hết hạn hoặc không hợp lệ.", result.Error.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTokenIsValid_ShouldUpdatePasswordAndRevokeTokens()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var validToken = "valid_token_123";
        using var context = CreateDbContext();
        
        context.Users.Add(new User 
        { 
            Id = userId,
            Email = "user@example.com", 
            PasswordResetToken = validToken,
            PasswordResetExpiry = DateTime.UtcNow.AddMinutes(10), // Not expired
            PasswordHash = "OldHash"
        });
        await context.SaveChangesAsync();

        _mockPasswordHasher.Setup(x => x.Hash("NewPassword123!")).Returns("NewHash123!");

        var service = new ResetPasswordService(context, _mockPasswordHasher.Object, _mockRefreshTokenRepository.Object);
        var request = new ResetPasswordRequest { Token = validToken, NewPassword = "NewPassword123!" };

        // Act
        var result = await service.ExecuteAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        
        var updatedUser = await context.Users.FirstAsync(u => u.Id == userId);
        Assert.Equal("NewHash123!", updatedUser.PasswordHash);
        Assert.Null(updatedUser.PasswordResetToken);
        Assert.Null(updatedUser.PasswordResetExpiry);

        _mockRefreshTokenRepository.Verify(x => x.RevokeAllUserTokensAsync(userId, "Reset password", null, It.IsAny<CancellationToken>()), Times.Once);
    }
}
