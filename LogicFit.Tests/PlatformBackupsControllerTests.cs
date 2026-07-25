using LogicFit.Application.Common.Interfaces;
using LogicFit.Platform.API.Features.Backups;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LogicFit.Tests;

public class PlatformBackupsControllerTests
{
    [Fact]
    public void Status_returns_the_safe_backup_readiness_contract()
    {
        var expected = new BackupStatus(
            IsEnabled: true,
            IsReady: false,
            Format: "BACPAC",
            RetentionDays: 7,
            RunAtUtc: "02:00",
            BackupCount: 0,
            UnavailableReason: "Storage is not configured.");
        var controller = new PlatformBackupsController(new StubBackupService(expected));

        var result = controller.Status();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, ok.Value);
    }

    private sealed class StubBackupService(BackupStatus status) : IBackupService
    {
        public Task<BackupRecord> CreateAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public IReadOnlyList<BackupRecord> List() => [];
        public BackupStatus GetStatus() => status;
    }
}
