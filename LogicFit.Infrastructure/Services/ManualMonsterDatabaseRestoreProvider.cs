using LogicFit.Application.Common.Interfaces;

namespace LogicFit.Infrastructure.Services;

/// <summary>Monster Free restore is deliberately operator-only until capabilities are proven.</summary>
public sealed class ManualMonsterDatabaseRestoreProvider : IDatabaseRestoreProvider
{
    public DatabaseRestoreCapabilities GetCapabilities()
        => new(false, "ManualOnly", false, false, "Monster restore requires a privileged operator and is disabled in the application.");

    public Task<DatabaseRestoreResult> RestoreAsync(DatabaseRestoreRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(new DatabaseRestoreResult(false, null, null, "RESTORE_MANUAL_ONLY", "ManualMonster"));
}
