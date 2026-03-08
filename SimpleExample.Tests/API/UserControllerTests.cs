using FluentAssertions;
using Moq;
using Microsoft.AspNetCore.Mvc;
using SimpleExample.API.Controllers;
using SimpleExample.Application.DTOs;
using SimpleExample.Application.Interfaces;
using Xunit;
using SimpleExample.Domain.Entities;

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
            LastName = "Meikalainen",
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

    // TEHTaVa: Kirjoita itse testit seuraaville:
    // 1. Create - InvalidOperationException (duplicate) → 409 Conflict

    [Fact]
    public async Task Create_WhenDuplicateEmail_ShouldReturnConflict()
    {
        // Arrange
        CreateUserDto createDto = new CreateUserDto
        {
            FirstName = "Matti",
            LastName = "Meikalainen",
            Email = "matti@example.com"
        };


        _mockService
        .Setup(s => s.CreateAsync(createDto))
        .ThrowsAsync(new InvalidOperationException("duplicate"));


        // Act

        ActionResult<UserDto> result = await _controller.Create(createDto);

        // Assert
        ConflictObjectResult conflictResult = result.Result
         .Should().BeOfType<ConflictObjectResult>().Subject;

        _mockService.Verify(s => s.CreateAsync(createDto), Times.Once);

    }
    // 2. Create - ArgumentException (validation) → 400 BadRequest

    [Fact]
    public async Task Create_WhenValidationFails_ShouldReturnBadRequest()
    {
        // Arrange
        CreateUserDto createDto = new CreateUserDto
        {
            FirstName = "Ma",
            LastName = "Meikalainen",
            Email = "matti@example.com"
        };

        _mockService
            .Setup(s => s.CreateAsync(createDto))
            .ThrowsAsync(new ArgumentException("Invalid"));

        // Act
        ActionResult<UserDto> result = await _controller.Create(createDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    // 3. Update - onnistuu → 200 OK

    [Fact]
    public async Task Update_WhenUserExists_ShouldReturnOk()
    {
        // Arrange
        Guid userId = Guid.NewGuid();

        UserDto updatedUserDto = new UserDto
        {
            Id = userId,
            FirstName = "Matt",
            LastName = "Meikalainen",
            Email = "matti@example.com"
        };

        UpdateUserDto updateUserDto = new UpdateUserDto
        {
            FirstName = "Matti",
            LastName = "Meikalainen",
            Email = "matti@outlook.com"
        };

        _mockService
            .Setup(s => s.UpdateAsync(userId, updateUserDto))
            .ReturnsAsync(updatedUserDto);

        // Act
        ActionResult<UserDto> result = await _controller.Update(userId, updateUserDto);

        // Assert
        OkObjectResult okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        UserDto returnedUser = okResult.Value.Should().BeOfType<UserDto>().Subject;

        returnedUser.Id.Should().Be(userId);
        returnedUser.FirstName.Should().Be("Matt");
        returnedUser.LastName.Should().Be("Meikalainen");
        returnedUser.Email.Should().Be("matti@example.com");

        _mockService.Verify(s => s.UpdateAsync(userId, updateUserDto), Times.Once);

    }

    // 4. Update - kayttajaa ei loydy → 404 NotFound
    [Fact]
    public async Task Update_WhenUserNotFound_ShouldReturnNotFound()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        UpdateUserDto dto = new UpdateUserDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com"
        };

        _mockService
            .Setup(s => s.UpdateAsync(userId, dto))
            .ReturnsAsync((UserDto?)null);

        // Act
        ActionResult<UserDto> result = await _controller.Update(userId, dto);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();

        _mockService.Verify(s => s.UpdateAsync(userId, dto), Times.Once);
    }

    // 5. Update - ArgumentException → 400 BadRequest
    [Fact]
    public async Task Update_WhenArgumentExceptionThrown_ShouldReturnBadRequest()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        UpdateUserDto dto = new UpdateUserDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = "invalid"
        };

        _mockService
            .Setup(s => s.UpdateAsync(userId, dto))
            .ThrowsAsync(new ArgumentException("Invalid data"));

        // Act
        ActionResult<UserDto> result = await _controller.Update(userId, dto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();

        _mockService.Verify(s => s.UpdateAsync(userId, dto), Times.Once);

    }

    // 6. Delete - onnistuu → 204 NoContent
    [Fact]
    public async Task Delete_WhenUserExists_ShouldReturnNoContent()
    {
        // Arrange
        Guid userId = Guid.NewGuid();

        _mockService
            .Setup(s => s.DeleteAsync(userId))
            .ReturnsAsync(true);

        // Act
        IActionResult result = await _controller.Delete(userId);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _mockService.Verify(s => s.DeleteAsync(userId), Times.Once);

    }



    // 7. Delete - kayttajaa ei loydy → 404 NotFound
    [Fact]
    public async Task Delete_WhenUserNotFound_ShouldReturnNotFound()
    {
        // Arrange
        Guid userId = Guid.NewGuid();

        _mockService
            .Setup(s => s.DeleteAsync(userId))
            .ReturnsAsync(false);

        // Act
        IActionResult result = await _controller.Delete(userId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();

        _mockService.Verify(s => s.DeleteAsync(userId), Times.Once);

    }

}
