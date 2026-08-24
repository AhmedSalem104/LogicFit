using LogicFit.API.Features.Platform.PaymentRequests;
using LogicFit.Application.Features.Platform.PaymentRequests.DTOs;
using LogicFit.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using Xunit;

namespace LogicFit.Tests;

public sealed class PaymentProofReviewContractTests
{
    [Fact]
    public void Platform_payment_review_exposes_upload_current_stream_and_history_routes()
    {
        var methods = typeof(PlatformPaymentRequestsController).GetMethods();

        Assert.Contains(methods, method => method.GetCustomAttributes<HttpPostAttribute>()
            .Any(attribute => attribute.Template == "{id:guid}/proof"));
        Assert.Contains(methods, method => method.GetCustomAttributes<HttpGetAttribute>()
            .Any(attribute => attribute.Template == "{id:guid}/proof"));
        Assert.Contains(methods, method => method.GetCustomAttributes<HttpGetAttribute>()
            .Any(attribute => attribute.Template == "{id:guid}/proofs"));
    }

    [Fact]
    public void Proof_metadata_contract_never_exposes_a_storage_key()
    {
        var properties = typeof(PaymentProofDto).GetProperties()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.DoesNotContain("StorageKey", properties);
        Assert.Contains("Sha256", properties);
        Assert.Contains("Version", properties);
        Assert.Contains("IsCurrent", properties);
    }

    [Fact]
    public void Upload_contract_retains_previous_versions_and_marks_only_the_new_one_current()
    {
        var handlerPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "LogicFit.Application", "Features", "Platform", "PaymentRequests", "Commands", "UploadPaymentProof", "UploadPaymentProofCommandHandler.cs");
        var source = File.ReadAllText(Path.GetFullPath(handlerPath));

        Assert.Contains("proof.IsCurrent = false", source, StringComparison.Ordinal);
        Assert.Contains("_context.PaymentProofs.Add", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DeleteFileAsync", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Payment_request_dto_carries_the_current_proof_version()
    {
        Assert.NotNull(typeof(PaymentRequestDto).GetProperty(nameof(PaymentRequestDto.ProofVersion)));
        Assert.Equal(typeof(int), typeof(PaymentRequestDto).GetProperty(nameof(PaymentRequestDto.ProofVersion))!.PropertyType);
        Assert.NotNull(typeof(PaymentProof).GetProperty(nameof(PaymentProof.UploadedAtUtc)));
    }
}
