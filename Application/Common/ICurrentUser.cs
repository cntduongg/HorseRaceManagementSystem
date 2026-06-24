namespace Application.Common;

/// <summary>
/// Abstraction over the authenticated user of the current request.
/// Implemented in the Api layer (reads claims from HttpContext) so handlers
/// never have to trust identity values coming from the request body.
/// </summary>
public interface ICurrentUser
{
    /// <summary>UserId from the JWT (claim "sub"), or null when unauthenticated.</summary>
    int? UserId { get; }

    /// <summary>Email claim, or null when unauthenticated.</summary>
    string? Email { get; }

    /// <summary>Role code claim (e.g. ADMIN, HORSE_OWNER), or null when unauthenticated.</summary>
    string? Role { get; }

    /// <summary>True when the request carries a valid authenticated identity.</summary>
    bool IsAuthenticated { get; }
}
