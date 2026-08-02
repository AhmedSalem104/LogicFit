namespace LogicFit.Domain.Enums;

/// <summary>Server-side target selection for a platform backup batch.</summary>
public enum BackupScope
{
    Platform = 1,
    SelectedTenants = 2,
    AllGyms = 3,
    AllFreelance = 4,
    AllTenants = 5,
    FullSystem = 6
}
