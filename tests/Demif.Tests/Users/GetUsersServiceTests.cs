using Demif.Application.Abstractions.Repositories;
using Demif.Application.Features.Users.GetUsers;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using Moq;

namespace Demif.Tests.Users;

public class GetUsersServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly GetUsersService _service;

    public GetUsersServiceTests()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _service = new GetUsersService(_userRepoMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_PaginationDefaults_AppliedWhenInvalid()
    {
        // Arrange
        var request = new GetUsersRequest { Page = -1, PageSize = 200 };
        _userRepoMock.Setup(r => r.GetPagedAsync(1, 100, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<User>(), 0));

        // Act
        var result = await _service.ExecuteAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Page);
        Assert.Equal(100, result.Value.PageSize);
        _userRepoMock.Verify(r => r.GetPagedAsync(1, 100, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ValidRequest_ReturnsPagedUsers()
    {
        // Arrange
        var request = new GetUsersRequest { Page = 1, PageSize = 10 };
        var users = new List<User>
        {
            new User { Id = Guid.NewGuid(), Email = "test1@example.com", Username = "Test 1", Status = UserStatus.Active },
            new User { Id = Guid.NewGuid(), Email = "test2@example.com", Username = "Test 2", Status = UserStatus.Banned }
        };

        _userRepoMock.Setup(r => r.GetPagedAsync(1, 10, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((users, 2));

        // Act
        var result = await _service.ExecuteAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Equal(2, result.Value.Users.Count);
        Assert.Equal("test1@example.com", result.Value.Users[0].Email);
        Assert.Equal("Active", result.Value.Users[0].Status);
        Assert.Equal("Banned", result.Value.Users[1].Status);
    }
}
