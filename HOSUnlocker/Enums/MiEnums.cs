namespace HOSUnlock.Enums;

public static class MiEnums
{
    public enum MiIsPassState
    {
        Unknown = -1,
        RequestApproved = 1,
        MaybeCanProceed = 4
    }

    public enum MiButtonState
    {
        Unknown = -1,
        RequestSubmissionPossible = 1,
        AccountBlockedFromApplyingUntilDate = 2,
        AccountCreatedLessThan30DaysAgo = 3
    }

    public enum MiAuthApplicationResult
    {
        Unknown = -1,
        ApplicationApproved = 1,
        ApplicationMaybeApproved = 2,
        ApplicationRejected = 3,
        ApplicationUnderReview = 4,
        ApplicationLimitReached = 5,
        ApplicationError = 6
    }

    public enum MiApplyResult
    {
        Unknown = -1,
        ApplicationSuccessful = 1,
        LimitReached = 3,
        BlockedUntil = 4
    }
}
