using SimpleExample.Domain.Entities;

namespace SimpleExample.Application.Interfaces;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
}
