using SimpleExample.Application.Interfaces;
using SimpleExample.Domain.Entities;

namespace SimpleExample.Infrastructure.Repositories;

/// <summary>
/// In-memory implementation of user repository for testing and demo purposes.
/// No database required - data is stored in memory and initialized with sample data.
/// </summary>
public class InMemoryUserRepository : IUserRepository
{
    private readonly List<User> _users;
    private readonly object _lock = new object();

    public InMemoryUserRepository()
    {
        _users = new List<User>();
        InitializeSampleData();
    }

    private void InitializeSampleData()
    {
        DateTime now = DateTime.UtcNow;

        _users.AddRange(new[]
        {
            new User
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                FirstName = "Matti",
                LastName = "Meikäläinen",
                Email = "matti.meikalainen@example.com",
                CreatedAt = now.AddDays(-30),
                UpdatedAt = now.AddDays(-30)
            },
            new User
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                FirstName = "Maija",
                LastName = "Virtanen",
                Email = "maija.virtanen@example.com",
                CreatedAt = now.AddDays(-15),
                UpdatedAt = now.AddDays(-5)
            },
            new User
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                FirstName = "Teppo",
                LastName = "Testaaja",
                Email = "teppo.testaaja@example.com",
                CreatedAt = now.AddDays(-7),
                UpdatedAt = now.AddDays(-1)
            }
        });
    }

    public Task<User?> GetByIdAsync(Guid id)
    {
        lock (_lock)
        {
            User? user = _users.FirstOrDefault(u => u.Id == id);
            return Task.FromResult(user);
        }
    }

    public Task<IEnumerable<User>> GetAllAsync()
    {
        lock (_lock)
        {
            IEnumerable<User> users = _users.ToList();
            return Task.FromResult(users);
        }
    }

    public Task<User> AddAsync(User entity)
    {
        lock (_lock)
        {
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            _users.Add(entity);
            return Task.FromResult(entity);
        }
    }

    public Task<User> UpdateAsync(User entity)
    {
        lock (_lock)
        {
            User? existingUser = _users.FirstOrDefault(u => u.Id == entity.Id);
            if (existingUser != null)
            {
                existingUser.FirstName = entity.FirstName;
                existingUser.LastName = entity.LastName;
                existingUser.Email = entity.Email;
                existingUser.UpdatedAt = DateTime.UtcNow;
                return Task.FromResult(existingUser);
            }

            return Task.FromResult(entity);
        }
    }

    public Task DeleteAsync(Guid id)
    {
        lock (_lock)
        {
            User? user = _users.FirstOrDefault(u => u.Id == id);
            if (user != null)
            {
                _users.Remove(user);
            }
            return Task.CompletedTask;
        }
    }

    public Task<bool> ExistsAsync(Guid id)
    {
        lock (_lock)
        {
            bool exists = _users.Any(u => u.Id == id);
            return Task.FromResult(exists);
        }
    }

    public Task<User?> GetByEmailAsync(string email)
    {
        lock (_lock)
        {
            User? user = _users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(user);
        }
    }
}
