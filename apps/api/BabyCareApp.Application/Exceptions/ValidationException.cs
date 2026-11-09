namespace BabyCareApp.Application.Exceptions;

/// <summary>For business-rule validation beyond what data-annotation attributes on a
/// request DTO can express (e.g. cross-field or cross-item checks) — ASP.NET Core's
/// built-in model validation already handles simple per-field rules via [ApiController],
/// so this is only for the rest.</summary>
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message)
    {
    }
}
