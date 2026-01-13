namespace HOSUnlock.Enums
{
    public class MiEnums
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
            ApplicationRejected,
            ApplicationUnderReview,
            ApplicationLimitReached,
            ApplicationError
        }

        public enum MiApplyResult
        {
            Unknown = -1,
            ApplicationSuccessful = 1,
            LimitReached = 3,
            BlockedUntil = 4
        }
    }
}
