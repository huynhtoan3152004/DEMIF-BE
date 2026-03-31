using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Abstractions.Services;
using Demif.Application.Features.Users.CreateUser;
using Demif.Domain.Entities;
using Moq;

namespace Demif.Tests.Users;

public class CreateUserServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IRoleRepository> _roleRepoMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly CreateUserService _service;

    public CreateUserServiceTests()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _roleRepoMock = new Mock<IRoleRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _dbContextMock = new Mock<IApplicationDbContext>();
        _service = new CreateUserService(
            _userRepoMock.Object, 
            _roleRepoMock.Object, 
            _passwordHasherMock.Object, 
            _dbContextMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_EmailExists_ReturnsConflict()
    {
        // Arrange
        var request = new CreateUserRequest { Email = "test@example.com", Username = "user1", Password = "pwd" };
        _userRepoMock.Setup(r => r.ExistsEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ExecuteAsync(request);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Conflict", result.Error.Code);
    }

    [Fact]
    public async Task ExecuteAsync_UsernameExists_ReturnsConflict()
    {
        // Arrange
        var request = new CreateUserRequest { Email = "test@example.com", Username = "user1", Password = "pwd" };
        _userRepoMock.Setup(r => r.ExistsEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userRepoMock.Setup(r => r.ExistsUsernameAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ExecuteAsync(request);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Conflict", result.Error.Code);
    }

    [Fact]
    public async Task ExecuteAsync_ValidDataWithoutRequestedRoles_AssignsDefaultRoleAndCreates()
    {
        // Arrange
        var request = new CreateUserRequest 
        { 
            Email = "new@example.com", 
            Username = "newuser", 
            Password = "password123",
            Roles = new List<string>() // no roles provided
        };

        var roleId = Guid.NewGuid();
        var defaultRole = new Role { Id = roleId, Name = "User" };

        _userRepoMock.Setup(r => r.ExistsEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _userRepoMock.Setup(r => r.ExistsUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _passwordHasherMock.Setup(p => p.Hash("password123")).Returns("hashed_pwd");
        
        // Return default role when requested
        _roleRepoMock.Setup(r => r.GetDefaultRoleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultRole);

        // Capture added user
        User addedUser = null!;
        _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, ct) => addedUser = u)
            .ReturnsAsync((User u, CancellationToken ct) => u);

        // Act
        var result = await _service.ExecuteAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        
        _userRepoMock.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        Assert.NotNull(addedUser);
        Assert.Equal("new@example.com", addedUser.Email);
        Assert.Equal("newuser", addedUser.Username);
        Assert.Equal("hashed_pwd", addedUser.PasswordHash);
        Assert.Single(addedUser.UserRoles);
        Assert.Equal(roleId, addedUser.UserRoles.First().RoleId);
    }
}
