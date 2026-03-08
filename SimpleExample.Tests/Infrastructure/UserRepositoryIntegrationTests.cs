using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SimpleExample.Domain.Entities;
using SimpleExample.Infrastructure.Data;
using SimpleExample.Infrastructure.Repositories;

using Xunit;

namespace SimpleExample.Tests.Infrastructure;

public class UserRepositoryIntegrationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly UserRepository _repository;

    public UserRepositoryIntegrationTests()
    {
        // Kayta in-memory databasea testaukseen
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new UserRepository(_context);
    }

    [Fact]
    public async Task AddAsync_ShouldAddUserToDatabase()
    {
        // Arrange
        User user = new User("Matti", "Meikalainen", "matti@example.com");

        // Act
        User result = await _repository.AddAsync(user);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);

        // Varmista etta tallentui tietokantaan
        User? savedUser = await _context.Users.FindAsync(result.Id);
        Assert.NotNull(savedUser);
        Assert.Equal("Matti", savedUser.FirstName);
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldFindUserByEmail()
    {
        // Arrange
        User user = new User("Matti", "Meikalainen", "test@example.com");
        await _repository.AddAsync(user);

        // Act
        User? result = await _repository.GetByEmailAsync("test@example.com");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Matti", result.FirstName);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserExists_ShouldReturnUser()
    {
        // Arrange
        User user = new User("Matti", "Meikalainen", "matti@example.com");
        await _repository.AddAsync(user);

        // Act
        User? result = await _repository.GetByIdAsync(user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
    }

    [Fact]
    public async Task GetAllAsync_WhenNoUsersExist_ShouldReturnEmptyList()
    {
        // Act
        IEnumerable<User> result = await _repository.GetAllAsync();

        // Assert
        Assert.NotNull(result);           // EI saa olla null
        Assert.Empty(result);             // Lista on tyhja
    }
    [Fact]
    public async Task UpdateAsync_ShouldUpdateUserInDatabase()
    {
        // Arrange
        var user = new User("Matti", "Meikalainen", "matti@example.com");
        await _repository.AddAsync(user);

        user.UpdateBasicInfo("Matt", "Damon");
        user.UpdateEmail("abc@hotmail.com");

        // Act
        var result = await _repository.UpdateAsync(user);

        // Assert
        var updatedUser = await _context.Users.FindAsync(result.Id);

        updatedUser.Should().NotBeNull();
        updatedUser!.FirstName.Should().Be("Matt");
        updatedUser.LastName.Should().Be("Damon");
        updatedUser.Email.Should().Be("abc@hotmail.com");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveUserFromDatabase()
    {
        // Arrange
        var user = new User("Matti", "Meikalainen", "matti@example.com");
        await _repository.AddAsync(user);

        // Act
        await _repository.DeleteAsync(user.Id);

        // Assert
        var deletedUser = await _context.Users.FindAsync(user.Id);
        deletedUser.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_WhenUserExists_ShouldReturnTrue()
    {
        // Arrange
        var user = new User("Matti", "Meikalainen", "matti@example.com");
        await _repository.AddAsync(user);

        // Act
        bool exists = await _repository.ExistsAsync(user.Id);

        // Assert
        exists.Should().BeTrue();
    }


    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
