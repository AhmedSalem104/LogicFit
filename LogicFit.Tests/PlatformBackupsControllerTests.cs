using LogicFit.Application.Common.Interfaces;
using LogicFit.API.Features.Platform.Backups;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
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

    [Fact]
    public void Download_returns_an_attachment_for_an_authorized_backup()
    {
        var controller = new PlatformBackupsController(new StubBackupService());

        var result = controller.Download("db60976-20260725-154918.bacpac");

        var file = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("db60976-20260725-154918.bacpac", file.FileDownloadName);
        Assert.Equal("application/octet-stream", file.ContentType);
    }

    [Fact]
    public void Download_returns_not_found_when_the_requested_backup_does_not_exist()
    {
        var controller = new PlatformBackupsController(new StubBackupService(isMissing: true));

        var result = controller.Download("db60976-20260725-154918.bacpac");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Batch_endpoint_forwards_server_side_scope_and_idempotency_contract()
    {
        var expected = new BackupBatchDto(Guid.NewGuid(), BackupScope.FullSystem, "Completed", null, null, null, []);
        var stub = new StubBackupService(batch: expected);
        var controller = new PlatformBackupsController(stub);

        var result = await controller.CreateBatch(
            new BackupBatchRequest(BackupScope.FullSystem, IdempotencyKey: "daily:test"),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, ok.Value);
        Assert.Equal("daily:test", stub.LastRequest?.IdempotencyKey);
    }

    [Fact]
    public async Task Batch_endpoint_maps_storage_failures_to_503_without_exposing_exception_details()
    {
        var controller = new PlatformBackupsController(
            new StubBackupService(batchException: new IOException(@"C:\private\connection-string.txt")));

        var result = await controller.CreateBatch(
            new BackupBatchRequest(BackupScope.Platform, IdempotencyKey: "storage-failure"),
            CancellationToken.None);

        var unavailable = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, unavailable.StatusCode);
        var payload = unavailable.Value!;
        Assert.Equal("BACKUP_SERVICE_UNAVAILABLE", payload.GetType().GetProperty("errorCode")?.GetValue(payload));
        Assert.DoesNotContain("connection-string", payload.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Batch_endpoint_preserves_safe_service_unavailable_code()
    {
        var controller = new PlatformBackupsController(
            new StubBackupService(batchException: new ServiceUnavailableException(
                "BACKUP_STORAGE_UNAVAILABLE", "Backup storage is temporarily unavailable.")));

        var result = await controller.CreateBatch(
            new BackupBatchRequest(BackupScope.Platform, IdempotencyKey: "service-failure"),
            CancellationToken.None);

        var unavailable = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, unavailable.StatusCode);
        var payload = unavailable.Value!;
        Assert.Equal("BACKUP_STORAGE_UNAVAILABLE", payload.GetType().GetProperty("errorCode")?.GetValue(payload));
    }

    [Fact]
    public async Task Retry_endpoint_returns_a_new_attempt_contract()
    {
        var expected = new BackupBatchDto(Guid.NewGuid(), BackupScope.Platform, "Running", null, null, null, []);
        var controller = new PlatformBackupsController(new StubBackupService(batch: expected));

        var result = await controller.Retry(Guid.NewGuid(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, ok.Value);
    }

    private sealed class StubBackupService(
        BackupStatus? status = null,
        bool isMissing = false,
        BackupBatchDto? batch = null,
        Exception? batchException = null) : IBackupService
    {
        public BackupBatchRequest? LastRequest { get; private set; }
        private readonly BackupBatchDto _batch = batch ?? new BackupBatchDto(Guid.NewGuid(), BackupScope.Platform, "Completed", null, null, null, []);
        private readonly Exception? _batchException = batchException;
        public Task<BackupRecord> CreateAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public IReadOnlyList<BackupRecord> List() => [];
        public BackupStatus GetStatus() => status ?? new BackupStatus(true, true, "BACPAC", 7, "02:00", 0, null);
        public Task<BackupBatchDto> CreateBatchAsync(BackupBatchRequest request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (_batchException is not null) return Task.FromException<BackupBatchDto>(_batchException);
            return Task.FromResult(_batch);
        }
        public Task<BackupBatchDto> RetryBatchAsync(Guid batchId, CancellationToken cancellationToken) => Task.FromResult(_batch);
        public IReadOnlyList<BackupBatchDto> ListBatches(int take = 50) => [_batch];
        public BackupDownload OpenRead(string fileName)
        {
            if (isMissing) throw new FileNotFoundException();
            return new BackupDownload(fileName, 1, new MemoryStream([0x1]));
        }
    }
}
