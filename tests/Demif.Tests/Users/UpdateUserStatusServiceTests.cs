using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Common.Models;
using Demif.Application.Features.Users.UpdateUserStatus;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using Moq;

namespace Demif.Tests.Users;

public class UpdateUserStatusServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepoMock;
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly UpdateUserStatusService _service;

    public UpdateUserStatusServiceTests()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _refreshTokenRepoMock = new Mock<IRefreshTokenRepository>();
        _dbContextMock = new Mock<IApplicationDbContext>();
        _service = new UpdateUserStatusService(_userRepoMock.Object, _refreshTokenRepoMock.Object, _dbContextMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidStatusString_ReturnsValidationFailure()
    {
        // Arrange
        var request = new UpdateUserStatusRequest { Status = "InvalidStatus" };

        // Act
        var result = await _service.ExecuteAsync(Guid.NewGuid(), request);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Validation", result.Error.Code);
    }

    [Fact]
    public async Task ExecuteAsync_UserNotFound_ReturnsNotFoundFailure()
    {
        // Arrange
        var request = new UpdateUserStatusRequest { Status = "Banned" };
        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _service.ExecuteAsync(Guid.NewGuid(), request);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("NotFound", result.Error.Code);
    }

    [Fact]
    public async Task ExecuteAsync_ValidStatusChangeToBanned_RevokesTokensAndSaves()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new UpdateUserStatusRequest { Status = "Banned" };
        var user = new User { Id = userId, Status = UserStatus.Active };

        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.ExecuteAsync(userId, request, "127.0.0.1");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(UserStatus.Banned, user.Status);
        
        _refreshTokenRepoMock.Verify(r => r.RevokeAllUserTokensAsync(
            userId, 
            It.IsAny<string>(), 
            "127.0.0.1", 
            It.IsAny<CancellationToken>()), Times.Once);

        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
