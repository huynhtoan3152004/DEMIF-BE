using Demif.Application.Abstractions.Repositories;
using Demif.Application.Features.Users.GetUserById;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using Moq;

namespace Demif.Tests.Users;

public class GetUserByIdServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly GetUserByIdService _service;

    public GetUserByIdServiceTests()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _service = new GetUserByIdService(_userRepoMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_UserNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepoMock.Setup(r => r.GetByIdWithRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _service.ExecuteAsync(userId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("NotFound", result.Error.Code);
    }

    [Fact]
    public async Task ExecuteAsync_UserExists_ReturnsUserDetails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            Username = "Test User",
            Status = UserStatus.Active,
            CurrentLevel = Level.Intermediate,
            UserRoles = new List<UserRole>
            {
                new UserRole 
                { 
                    RoleId = Guid.NewGuid(), 
                    Role = new Role { Name = "Admin" }
                }
            }
        };

        _userRepoMock.Setup(r => r.GetByIdWithRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.ExecuteAsync(userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(userId, result.Value.Id);
        Assert.Equal("test@example.com", result.Value.Email);
        Assert.Equal("Test User", result.Value.Username);
        Assert.Equal("Active", result.Value.Status);
        Assert.Equal("Intermediate", result.Value.CurrentLevel);
        Assert.Single(result.Value.Roles);
        Assert.Equal("Admin", result.Value.Roles[0].RoleName);
    }
}
