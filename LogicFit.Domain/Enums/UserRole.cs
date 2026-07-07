namespace LogicFit.Domain.Enums;

public enum UserRole
{
    Owner = 1,
    Coach = 2,
    Client = 3,
    Manager = 4,
    Receptionist = 5,
    Accountant = 6,
    Trainer = 7,

    // Platform-layer roles (users belong to the sentinel platform tenant)
    PlatformOwner = 8,
    PlatformAdmin = 9
}
