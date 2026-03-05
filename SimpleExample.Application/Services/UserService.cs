using SimpleExample.Application.DTOs;
using SimpleExample.Application.Interfaces;
using SimpleExample.Domain.Entities;

namespace SimpleExample.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        User? user = await _userRepository.GetByIdAsync(id);
        return user == null ? null : MapToDto(user);
    }

    public async Task<IEnumerable<UserDto>> GetAllAsync()
    {
        IEnumerable<User> users = await _userRepository.GetAllAsync();
        return users.Select(MapToDto);
    }

    public async Task<UserDto> CreateAsync(CreateUserDto createUserDto)
    {
        User user = new User
        {
            FirstName = createUserDto.FirstName,
            LastName = createUserDto.LastName,
            Email = createUserDto.Email
        };

        User createdUser = await _userRepository.AddAsync(user);
        return MapToDto(createdUser);
    }

    public async Task<UserDto?> UpdateAsync(Guid id, UpdateUserDto updateUserDto)
    {
        User? user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            return null;
        }

        user.FirstName = updateUserDto.FirstName;
        user.LastName = updateUserDto.LastName;
        user.Email = updateUserDto.Email;

        User updatedUser = await _userRepository.UpdateAsync(user);
        return MapToDto(updatedUser);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        bool exists = await _userRepository.ExistsAsync(id);
        if (!exists)
        {
            return false;
        }

        await _userRepository.DeleteAsync(id);
        return true;
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}
