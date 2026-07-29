using LogicFit.Domain.Enums;
using LogicFit.Domain.Services;
using Xunit;

namespace LogicFit.Tests;

public class ApplicationRequestStateMachineTests
{
    [Theory]
    [InlineData(ApplicationRequestStatus.Draft, ApplicationRequestStatus.Submitted)]
    [InlineData(ApplicationRequestStatus.Submitted, ApplicationRequestStatus.UnderReview)]
    [InlineData(ApplicationRequestStatus.UnderReview, ApplicationRequestStatus.NeedsMoreInformation)]
    [InlineData(ApplicationRequestStatus.UnderReview, ApplicationRequestStatus.Approved)]
    [InlineData(ApplicationRequestStatus.NeedsMoreInformation, ApplicationRequestStatus.Submitted)]
    public void Allows_only_documented_forward_transitions(ApplicationRequestStatus from, ApplicationRequestStatus to)
        => Assert.True(ApplicationRequestStateMachine.CanTransition(from, to));

    [Theory]
    [InlineData(ApplicationRequestStatus.Draft, ApplicationRequestStatus.Approved)]
    [InlineData(ApplicationRequestStatus.Submitted, ApplicationRequestStatus.Approved)]
    [InlineData(ApplicationRequestStatus.Rejected, ApplicationRequestStatus.Submitted)]
    [InlineData(ApplicationRequestStatus.Approved, ApplicationRequestStatus.UnderReview)]
    public void Rejects_unsafe_or_terminal_transitions(ApplicationRequestStatus from, ApplicationRequestStatus to)
        => Assert.False(ApplicationRequestStateMachine.CanTransition(from, to));

    [Theory]
    [InlineData(ApplicationRequestStatus.Draft, true)]
    [InlineData(ApplicationRequestStatus.NeedsMoreInformation, true)]
    [InlineData(ApplicationRequestStatus.Approved, false)]
    [InlineData(ApplicationRequestStatus.Rejected, false)]
    public void Identifies_active_duplicate_prevention_states(ApplicationRequestStatus status, bool expected)
        => Assert.Equal(expected, ApplicationRequestStateMachine.IsActive(status));
}
