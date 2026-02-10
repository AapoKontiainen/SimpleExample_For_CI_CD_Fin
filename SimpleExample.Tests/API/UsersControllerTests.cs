using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SimpleExample.API.Controllers;
using SimpleExample.Application.DTOs;
using SimpleExample.Application.Interfaces;
using Xunit;

namespace SimpleExample.Tests.API;

public class UsersControllerTests
{
    private readonly Mock<IUserService> _mockService;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _mockService = new Mock<IUserService>();
        _controller = new UsersController(_mockService.Object);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOkWithUsers()
    {
        // Arrange
        List<UserDto> users = new List<UserDto>
        {
            new UserDto { Id = Guid.NewGuid(), FirstName = "Matti", LastName = "M", Email = "m@m.com" },
            new UserDto { Id = Guid.NewGuid(), FirstName = "Maija", LastName = "V", Email = "m@v.com" }
        };

        _mockService
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(users);

        // Act
        ActionResult<IEnumerable<UserDto>> result = await _controller.GetAll();

        // Assert
        OkObjectResult okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        IEnumerable<UserDto> returnedUsers = okResult.Value.Should().BeAssignableTo<IEnumerable<UserDto>>().Subject;
        returnedUsers.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetById_WhenUserExists_ShouldReturnOk()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        UserDto user = new UserDto { Id = userId, FirstName = "Matti", LastName = "M", Email = "m@m.com" };

        _mockService
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        ActionResult<UserDto> result = await _controller.GetById(userId);

        // Assert
        OkObjectResult okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        UserDto returnedUser = okResult.Value.Should().BeOfType<UserDto>().Subject;
        returnedUser.Id.Should().Be(userId);
    }

    [Fact]
    public async Task GetById_WhenUserNotFound_ShouldReturnNotFound()
    {
        // Arrange
        Guid userId = Guid.NewGuid();

        _mockService
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync((UserDto?)null);

        // Act
        ActionResult<UserDto> result = await _controller.GetById(userId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        CreateUserDto createDto = new CreateUserDto
        {
            FirstName = "Matti",
            LastName = "Meikäläinen",
            Email = "matti@example.com"
        };

        UserDto createdUser = new UserDto
        {
            Id = Guid.NewGuid(),
            FirstName = createDto.FirstName,
            LastName = createDto.LastName,
            Email = createDto.Email
        };

        _mockService
            .Setup(x => x.CreateAsync(createDto))
            .ReturnsAsync(createdUser);

        // Act
        ActionResult<UserDto> result = await _controller.Create(createDto);

        // Assert
        CreatedAtActionResult createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        UserDto returnedUser = createdResult.Value.Should().BeOfType<UserDto>().Subject;
        returnedUser.FirstName.Should().Be("Matti");
    }

    [Fact]
    public async Task Create_WhenDuplicate_ThrowsInvalidOperationException_ReturnsConflict()
    {
        // Arrange
        var createDto = new CreateUserDto { FirstName = "Matti", LastName = "Meikäläinen", Email = "matti@example.com" };
        _mockService.Setup(x => x.CreateAsync(createDto)).ThrowsAsync(new InvalidOperationException("Käyttäjä on jo olemassa"));

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var conflictResult = result.Result.Should().BeOfType<ConflictObjectResult>().Subject;
        var value = conflictResult.Value;
        value.Should().NotBeNull();
        value.GetType().GetProperty("message").GetValue(value).Should().Be("Käyttäjä on jo olemassa");
    }

    [Fact]
    public async Task Create_WhenArgumentException_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateUserDto { FirstName = "", LastName = "", Email = "invalid" };
        _mockService.Setup(x => x.CreateAsync(createDto)).ThrowsAsync(new ArgumentException("Virheellinen data"));

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var value = badRequest.Value;
        value.Should().NotBeNull();
        value.GetType().GetProperty("message").GetValue(value).Should().Be("Virheellinen data");
    }

    [Fact]
    public async Task Update_WhenSuccess_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateDto = new UpdateUserDto { FirstName = "Matti", LastName = "Meikäläinen", Email = "matti@example.com" };
        var updatedUser = new UserDto { Id = userId, FirstName = "Matti", LastName = "Meikäläinen", Email = "matti@example.com" };
        _mockService.Setup(x => x.UpdateAsync(userId, updateDto)).ReturnsAsync(updatedUser);

        // Act
        var result = await _controller.Update(userId, updateDto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedUser = okResult.Value.Should().BeOfType<UserDto>().Subject;
        returnedUser.Id.Should().Be(userId);
    }

    [Fact]
    public async Task Update_WhenUserNotFound_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateDto = new UpdateUserDto { FirstName = "Test", LastName = "User", Email = "test@example.com" };
        _mockService.Setup(x => x.UpdateAsync(userId, updateDto)).ReturnsAsync((UserDto?)null);

        // Act
        var result = await _controller.Update(userId, updateDto);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Update_WhenArgumentException_ReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateDto = new UpdateUserDto { FirstName = "", LastName = "", Email = "invalid" };
        _mockService.Setup(x => x.UpdateAsync(userId, updateDto)).ThrowsAsync(new ArgumentException("Virheellinen data"));

        // Act
        var result = await _controller.Update(userId, updateDto);

        // Assert
        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var value = badRequest.Value;
        value.Should().NotBeNull();
        value.GetType().GetProperty("message").GetValue(value).Should().Be("Virheellinen data");
    }

    [Fact]
    public async Task Delete_WhenSuccess_ReturnsNoContent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockService.Setup(x => x.DeleteAsync(userId)).ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(userId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_WhenUserNotFound_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockService.Setup(x => x.DeleteAsync(userId)).ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(userId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }
}