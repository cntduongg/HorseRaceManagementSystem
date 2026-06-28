//using System.Security.Claims;
//using Application.Common.Interfaces;
//using Microsoft.AspNetCore.Http;

//namespace Infrastructure.Services;

//public sealed class CurrentUser : ICurrentUser
//{
//    private readonly IHttpContextAccessor _httpContextAccessor;

//    public CurrentUser(IHttpContextAccessor httpContextAccessor)
//    {
//        _httpContextAccessor = httpContextAccessor;
//    }

//    public int? UserId
//    {
//        get
//        {
//            var value =
//                _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
//                ?? _httpContextAccessor.HttpContext?.User.FindFirstValue("sub");

//            return int.TryParse(value, out var id)
//                ? id
//                : null;
//        }
//    }

//    public bool IsInRole(string role)
//    {
//        return _httpContextAccessor.HttpContext?.User.IsInRole(role) == true;
//    }
//}