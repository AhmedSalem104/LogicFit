using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.Users.DTOs;

public class UserDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public decimal WalletBalance { get; set; }
    public UserProfileDto? Profile { get; set; }
}

public class UserProfileDto
{
    public string? FullName { get; set; }
    public int? Gender { get; set; }
    public DateTime? BirthDate { get; set; }
    public double? HeightCm { get; set; }
    public string? ActivityLevel { get; set; }
    public string? MedicalHistory { get; set; }
}

public class CreateUserDto
{
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Password { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string? FullName { get; set; }
}

public class UpdateUserDto
{
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
}

public class UpdateUserProfileDto
{
    public string? FullName { get; set; }
    public int? Gender { get; set; }
    public DateTime? BirthDate { get; set; }
    public double? HeightCm { get; set; }
    public string? ActivityLevel { get; set; }
    public string? MedicalHistory { get; set; }
}
