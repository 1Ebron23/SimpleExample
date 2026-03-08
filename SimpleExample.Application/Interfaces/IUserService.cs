using SimpleExample.Application.DTOs;

namespace SimpleExample.Application.Interfaces;

public interface IUserService
{
    Task<UserDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<UserDto>> GetAllAsync();
    Task<UserDto?> CreateAsync(CreateUserDto createUserDto);
    Task<UserDto?> UpdateAsync(Guid id, UpdateUserDto updateUserDto);
    Task<bool> DeleteAsync(Guid id);
}
