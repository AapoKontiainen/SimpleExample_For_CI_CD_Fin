using FluentAssertions;
using Moq;
using SimpleExample.Application.DTOs;
using SimpleExample.Application.Interfaces;
using SimpleExample.Application.Services;
using SimpleExample.Domain.Entities;
using Xunit;

namespace SimpleExample.Tests.Application;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _mockRepository;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _mockRepository = new Mock<IUserRepository>();
        _service = new UserService(_mockRepository.Object);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreateUser()
    {
        // Arrange
        CreateUserDto dto = new CreateUserDto
        {
            FirstName = "Matti",
            LastName = "Meikäläinen",
            Email = "matti@example.com"
        };

        // Mock: Email ei ole käytössä
        _mockRepository
            .Setup(x => x.GetByEmailAsync(dto.Email))
            .ReturnsAsync((User?)null);

        _mockRepository
            .Setup(x => x.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        // Act
        UserDto result = await _service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.FirstName.Should().Be("Matti");
        result.LastName.Should().Be("Meikäläinen");
        result.Email.Should().Be("matti@example.com");

        // Varmista että AddAsync kutsuttiin kerran
        _mockRepository.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEmail_ShouldThrowInvalidOperationException()
    {
        // Arrange
        CreateUserDto dto = new CreateUserDto
        {
            FirstName = "Matti",
            LastName = "Meikäläinen",
            Email = "existing@example.com"
        };

        User existingUser = new User("Maija", "Virtanen", "existing@example.com");

        // Mock: Email on jo käytössä!
        _mockRepository
            .Setup(x => x.GetByEmailAsync(dto.Email))
            .ReturnsAsync(existingUser);

        // Act
        Func<Task> act = async () => await _service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*jo olemassa*");

        // Varmista että AddAsync EI kutsuttu
        _mockRepository.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_UserExists_ReturnsUserDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User("Matti", "Meikäläinen", "matti@example.com") { Id = userId };
        _mockRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act
        var result = await _service.GetByIdAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.FirstName.Should().Be("Matti");
        result.LastName.Should().Be("Meikäläinen");
        result.Email.Should().Be("matti@example.com");
    }

    [Fact]
    public async Task GetByIdAsync_UserDoesNotExist_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act
        var result = await _service.GetByIdAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsListOfUsers()
    {
        // Arrange
        var user1 = new User("Matti", "Meikäläinen", "matti@example.com") { Id = Guid.NewGuid() };
        var user2 = new User("Maija", "Virtanen", "maija@example.com") { Id = Guid.NewGuid() };
        var users = new List<User> { user1, user2 };
        _mockRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(users);

        // Act
        var result = (await _service.GetAllAsync()).ToList();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].FirstName.Should().Be("Matti");
        result[1].FirstName.Should().Be("Maija");
    }

    [Fact]
    public async Task UpdateAsync_UserExists_UpdatesAndReturnsUserDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User("Matti", "Meikäläinen", "matti@example.com") { Id = userId };
        var updateDto = new UpdateUserDto
        {
            FirstName = "MattiUpdated",
            LastName = "MeikäläinenUpdated",
            Email = "matti.updated@example.com"
        };
        _mockRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        // Act
        var result = await _service.UpdateAsync(userId, updateDto);

        // Assert
        result.Should().NotBeNull();
        result!.FirstName.Should().Be("MattiUpdated");
        result.LastName.Should().Be("MeikäläinenUpdated");
        result.Email.Should().Be("matti.updated@example.com");
        _mockRepository.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_UserDoesNotExist_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateDto = new UpdateUserDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com"
        };
        _mockRepository.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act
        var result = await _service.UpdateAsync(userId, updateDto);

        // Assert
        result.Should().BeNull();
        _mockRepository.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_UserExists_DeletesAndReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockRepository.Setup(x => x.ExistsAsync(userId)).ReturnsAsync(true);
        _mockRepository.Setup(x => x.DeleteAsync(userId)).Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(userId);

        // Assert
        result.Should().BeTrue();
        _mockRepository.Verify(x => x.DeleteAsync(userId), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_UserDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockRepository.Setup(x => x.ExistsAsync(userId)).ReturnsAsync(false);

        // Act
        var result = await _service.DeleteAsync(userId);

        // Assert
        result.Should().BeFalse();
        _mockRepository.Verify(x => x.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }
}