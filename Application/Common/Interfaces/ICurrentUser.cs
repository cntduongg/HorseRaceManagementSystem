using System.Security.Claims;

namespace Application.Common.Interfaces;

public interface ICurrentUser
{
    int? UserId { get; }

    bool IsInRole(string role);
}