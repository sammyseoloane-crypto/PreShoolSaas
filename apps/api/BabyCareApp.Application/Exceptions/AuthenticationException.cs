namespace BabyCareApp.Application.Exceptions;

/// <summary>Invalid credentials, invalid/expired/revoked refresh token, etc.</summary>
public class AuthenticationException : Exception
{
    public AuthenticationException(string message) : base(message)
    {
    }
}
