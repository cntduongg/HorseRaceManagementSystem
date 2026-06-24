using System.Security.Claims;
using Application.Common;

namespace Api.Services;

/// <summary>
/// Reads the authenticated user's claims from the current HTTP request.
/// The default JWT handler maps "sub" to <see cref="ClaimTypes.NameIdentifier"/>,
/// so both keys are checked for robustness.
/// </summary>
public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public int? UserId
    {
        get
        {
            var value = Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? Principal?.FindFirstValue("sub");

            return int.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Email =>
        Principal?.FindFirstValue(ClaimTypes.Email) ?? Principal?.FindFirstValue("email");

    public string? Role =>
        Principal?.FindFirstValue(ClaimTypes.Role) ?? Principal?.FindFirstValue("role");
}
