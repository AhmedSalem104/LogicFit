namespace LogicFit.Domain.Enums;

public enum OtpChallengeStatus
{
    Pending = 1,
    Consumed = 2,
    Expired = 3,
    Revoked = 4,
    Locked = 5,
    Failed = 6
}
