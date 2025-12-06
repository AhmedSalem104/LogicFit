namespace LogicFit.Application.Features.Clients.DTOs;

public class ClientDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string? Email { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public decimal WalletBalance { get; set; }
    public ClientProfileDto? Profile { get; set; }
    public ClientSubscriptionInfoDto? ActiveSubscription { get; set; }
}

public class ClientProfileDto
{
    public string? FullName { get; set; }
    public int? Gender { get; set; }
    public DateTime? BirthDate { get; set; }
    public double? HeightCm { get; set; }
    public string? ActivityLevel { get; set; }
    public string? MedicalHistory { get; set; }
}

public class ClientSubscriptionInfoDto
{
    public Guid Id { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CreateClientDto
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Password { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public int? Gender { get; set; }
    public DateTime? BirthDate { get; set; }
    public double? HeightCm { get; set; }
    public string? ActivityLevel { get; set; }
    public string? MedicalHistory { get; set; }
}

public class UpdateClientDto
{
    public string? Email { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? FullName { get; set; }
    public int? Gender { get; set; }
    public DateTime? BirthDate { get; set; }
    public double? HeightCm { get; set; }
    public string? ActivityLevel { get; set; }
    public string? MedicalHistory { get; set; }
}
