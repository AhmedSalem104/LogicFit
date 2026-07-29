using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Services;

/// <summary>
/// Valid lifecycle transitions for platform-reviewed onboarding requests. Rejected requests remain
/// immutable evidence of the decision; a later attempt is a new request linked to the old one.
/// </summary>
public static class ApplicationRequestStateMachine
{
    private static readonly IReadOnlyDictionary<ApplicationRequestStatus, ApplicationRequestStatus[]> Allowed =
        new Dictionary<ApplicationRequestStatus, ApplicationRequestStatus[]>
        {
            [ApplicationRequestStatus.Draft] = [ApplicationRequestStatus.Submitted, ApplicationRequestStatus.Cancelled],
            [ApplicationRequestStatus.Submitted] = [ApplicationRequestStatus.UnderReview, ApplicationRequestStatus.Cancelled, ApplicationRequestStatus.Expired],
            [ApplicationRequestStatus.UnderReview] = [ApplicationRequestStatus.NeedsMoreInformation, ApplicationRequestStatus.Approved, ApplicationRequestStatus.Rejected, ApplicationRequestStatus.Expired],
            [ApplicationRequestStatus.NeedsMoreInformation] = [ApplicationRequestStatus.Submitted, ApplicationRequestStatus.Cancelled, ApplicationRequestStatus.Expired],
            [ApplicationRequestStatus.Approved] = [],
            [ApplicationRequestStatus.Rejected] = [],
            [ApplicationRequestStatus.Cancelled] = [],
            [ApplicationRequestStatus.Expired] = []
        };

    public static bool CanTransition(ApplicationRequestStatus from, ApplicationRequestStatus to)
        => from == to || (Allowed.TryGetValue(from, out var targets) && targets.Contains(to));

    public static bool IsActive(ApplicationRequestStatus status) =>
        status is ApplicationRequestStatus.Draft
            or ApplicationRequestStatus.Submitted
            or ApplicationRequestStatus.UnderReview
            or ApplicationRequestStatus.NeedsMoreInformation;
}
