using LogicFit.API.Features.WorkspaceApplications;
using Xunit;

namespace LogicFit.Tests;

public sealed class WorkspaceApplicationPaymentProofContractTests
{
    [Fact]
    public void Public_tracking_surface_exposes_a_multipart_payment_proof_route()
    {
        var method = typeof(WorkspaceApplicationsController).GetMethod(nameof(WorkspaceApplicationsController.UploadTrackingPaymentProof));

        Assert.NotNull(method);
        Assert.Contains(
            method!.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.HttpPostAttribute), inherit: true)
                .Cast<Microsoft.AspNetCore.Mvc.HttpPostAttribute>(),
            attribute => attribute.Template == "tracking/payment-proof");
    }

    [Fact]
    public void Owner_upload_handler_resolves_payment_by_tracking_application_not_client_id()
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "LogicFit.Application", "Features", "WorkspaceApplications", "Commands",
            "UploadApplicationPaymentProof", "UploadApplicationPaymentProofCommandHandler.cs");
        var source = File.ReadAllText(Path.GetFullPath(path));

        Assert.Contains("ApplicationTrackingSessionResolver.GetActiveAsync", source, StringComparison.Ordinal);
        Assert.Contains("x.ApplicationRequestId == application.Id", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PaymentRequestId = request", source, StringComparison.Ordinal);
        Assert.Contains("WorkspaceApplicationPaymentProofUploaded", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Workspace_resubmission_fails_closed_without_a_payment_proof()
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "LogicFit.Application", "Features", "WorkspaceApplications", "Commands",
            "ResubmitApplication", "ResubmitApplicationCommandHandler.cs");
        var source = File.ReadAllText(Path.GetFullPath(path));

        Assert.Contains("PAYMENT_PROOF_REQUIRED", source, StringComparison.Ordinal);
        Assert.Contains("Include(x => x.Proofs)", source, StringComparison.Ordinal);
    }
}
