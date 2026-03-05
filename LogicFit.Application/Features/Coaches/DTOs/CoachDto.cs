namespace LogicFit.Application.Features.Coaches.DTOs;

public class CoachDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public CoachProfileDto? Profile { get; set; }
    public int TraineeCount { get; set; }
}

public class CoachProfileDto
{
    public string? FullName { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public int? Gender { get; set; }
    public DateTime? BirthDate { get; set; }
}
