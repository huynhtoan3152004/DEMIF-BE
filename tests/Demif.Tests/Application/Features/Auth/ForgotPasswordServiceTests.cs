using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Abstractions.Services;
using Demif.Application.Common.Models;
using Demif.Application.Features.Auth.ForgotPassword;
using Demif.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Demif.Tests.Application.Features.Auth;

public class ForgotPasswordServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly ForgotPasswordService _forgotPasswordService;

    public ForgotPasswordServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockDbContext = new Mock<IApplicationDbContext>();
        _mockEmailService = new Mock<IEmailService>();
        _mockConfiguration = new Mock<IConfiguration>();

        _mockConfiguration.Setup(x => x["App:FrontendUrl"]).Returns("http://localhost:3000");

        _forgotPasswordService = new ForgotPasswordService(
            _mockUserRepository.Object,
            _mockDbContext.Object,
            _mockEmailService.Object,
            _mockConfiguration.Object
        );
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserNotFound_ShouldReturnSuccess_ToPreventEnumeration()
    {
        // Arrange
        _mockUserRepository.Setup(x => x.GetByEmailAsync("notfound@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
            
        var request = new ForgotPasswordRequest { Email = "notfound@example.com" };

        // Act
        var result = await _forgotPasswordService.ExecuteAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        _mockEmailService.Verify(x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserIsGoogleOauth_ShouldSendResetEmail()
    {
        // Arrange
        var user = new User { Email = "google@example.com", AuthProvider = "google" };
        _mockUserRepository.Setup(x => x.GetByEmailAsync("google@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
            
        var request = new ForgotPasswordRequest { Email = "google@example.com" };

        // Act
        var result = await _forgotPasswordService.ExecuteAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(user.PasswordResetToken);
        Assert.NotNull(user.PasswordResetExpiry);
        _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockEmailService.Verify(x => x.SendPasswordResetEmailAsync("google@example.com", It.IsAny<string>(), It.Is<string>(url => url.Contains("reset-password?token=")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenValidUser_ShouldSetTokenAndExpiry_AndSendEmail()
    {
        // Arrange
        var user = new User { Email = "valid@example.com", Username = "TestUser", AuthProvider = "email" };
        _mockUserRepository.Setup(x => x.GetByEmailAsync("valid@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
            
        var request = new ForgotPasswordRequest { Email = "valid@example.com" };

        // Act
        var result = await _forgotPasswordService.ExecuteAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(user.PasswordResetToken);
        Assert.NotNull(user.PasswordResetExpiry);
        
        _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockEmailService.Verify(x => x.SendPasswordResetEmailAsync("valid@example.com", "TestUser", It.Is<string>(url => url.Contains("reset-password?token=")), It.IsAny<CancellationToken>()), Times.Once);
    }
}
