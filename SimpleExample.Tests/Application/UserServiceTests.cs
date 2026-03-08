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
            LastName = "Meikalainen",
            Email = "matti@example.com"
        };

        // Mock: Email ei ole kaytossa
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
        result.LastName.Should().Be("Meikalainen");
        result.Email.Should().Be("matti@example.com");

        // Varmista etta AddAsync kutsuttiin kerran
        _mockRepository.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEmail_ShouldThrowInvalidOperationException()
    {
        // Arrange
        CreateUserDto dto = new CreateUserDto
        {
            FirstName = "Matti",
            LastName = "Meikalainen",
            Email = "existing@example.com"
        };

        User existingUser = new User("Maija", "Virtanen", "existing@example.com");

        // Mock: Email on jo kaytossa!
        _mockRepository
            .Setup(x => x.GetByEmailAsync(dto.Email))
            .ReturnsAsync(existingUser);

        // Act
        Func<Task> act = async () => await _service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*jo olemassa*");

        // Varmista etta AddAsync EI kutsuttu
        _mockRepository.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);
    }

    // TEHTaVa: Kirjoita itse testit seuraaville:
    // 1. GetByIdAsync - loytyy
    [Fact]
    public async Task GetByIdAsync_WhenUserExists_ShouldReturnUserDto()
    {
        // Arrange
        
        var guid = Guid.NewGuid();

        var user = new User("Matti", "Meikalainen", "matti@example.com");
        _mockRepository.Setup(r => r.GetByIdAsync(guid)).ReturnsAsync(user);

        // Act
        var result = await _service.GetByIdAsync(guid);

        // Assert
        result.Should().NotBeNull();
        result.FirstName.Should().Be("Matti");
        result.LastName.Should().Be("Meikalainen");
        result.Email.Should().Be("matti@example.com");
    }

    // 2. GetByIdAsync - ei loydy
    [Fact]
    public async Task GetByIdAsync_WhenUserDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        
        var guid = Guid.NewGuid(); 
        
        _mockRepository.Setup(r => r.GetByIdAsync(guid)).ReturnsAsync((User?)null);

        // Act
        var result = await _service.GetByIdAsync(guid);

        // Assert
        result.Should().BeNull();
    }

    // 3. GetAllAsync - palauttaa listan
    [Fact]
    public async Task GetAllAsync_ShouldReturnListOfUsers()
    {
        // Arrange
        var users = new List<User>
    {
        new User("Matti", "Meikalainen", "matti@example.com"),
        new User("Maija", "Virtanen", "maija@example.com")
    };

        _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(users);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Select(x => x.Email).Should().Contain(new[] { "matti@example.com", "maija@example.com" });
    }

    // 4. UpdateAsync - onnistuu
    [Fact]
    public async Task UpdateAsync_WhenUserExists_ShouldUpdateAndReturnDto()
    {
        // Arrange
        var guid = Guid.NewGuid();

        var existing = new User("Old", "Name", "old@example.com");
        _mockRepository.Setup(r => r.GetByIdAsync(guid)).ReturnsAsync(existing);

        var dto = new UpdateUserDto
        {
            FirstName = "New",
            LastName = "Name",
            Email = "new@example.com"
        };

        _mockRepository.Setup(r => r.UpdateAsync(existing)).ReturnsAsync(existing);

        // Act
        var result = await _service.UpdateAsync(guid, dto);

        // Assert DTO
        result.Should().NotBeNull();
        result.FirstName.Should().Be("New");
        result.Email.Should().Be("new@example.com");

        // Assert DOMAIN ENTITY was updated
        existing.FirstName.Should().Be("New");
        existing.LastName.Should().Be("Name");
        existing.Email.Should().Be("new@example.com");

        // Assert repository call
        _mockRepository.Verify(r => r.UpdateAsync(existing), Times.Once);
    }


    // 5. UpdateAsync - kayttajaa ei loydy
    [Fact]
    public async Task UpdateAsync_WhenUserDoesNotExist_ShouldThrowException()
    {
        // Arrange

        var guid = Guid.NewGuid();

        _mockRepository.Setup(r => r.GetByIdAsync(guid))
                .ThrowsAsync(new InvalidOperationException("ei loydy"));

        var dto = new UpdateUserDto
        {
            FirstName = "New",
            LastName = "Name",
            Email = "new@example.com"
        };

        // Act
        Func<Task> act = async () => await _service.UpdateAsync(guid, dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ei loydy*");

        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    // 6. DeleteAsync - onnistuu
    [Fact]
    public async Task DeleteAsync_WhenUserExists_ShouldDeleteUser()
    {
        // Arrange

        var guid = Guid.NewGuid();

        _mockRepository.Setup(r => r.ExistsAsync(guid)).ReturnsAsync(true);

        // Act
        await _service.DeleteAsync(guid);

        // Assert
        _mockRepository.Verify(r => r.DeleteAsync(guid), Times.Once);
    }

    // 7. DeleteAsync - kayttajaa ei loydy
    [Fact]
    public async Task DeleteAsync_WhenUserDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        var guid = Guid.NewGuid();

        _mockRepository.Setup(r => r.ExistsAsync(guid)).ReturnsAsync(false);

        // Act
        var result = await _service.DeleteAsync(guid);

        // Assert
        result.Should().BeFalse();
        _mockRepository.Verify(r => r.DeleteAsync(guid), Times.Never);
    }



}