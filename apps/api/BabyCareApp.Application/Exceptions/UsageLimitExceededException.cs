namespace BabyCareApp.Application.Exceptions;

/// <summary>Thrown when an action would exceed a limit on the tenant's current subscription plan.</summary>
public class UsageLimitExceededException : Exception
{
    public UsageLimitExceededException(string message) : base(message)
    {
    }
}
