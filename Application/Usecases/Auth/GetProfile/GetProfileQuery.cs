using MediatR;

namespace Application.Usecases.Auth.GetProfile;

/// <summary>
/// Returns the profile of the currently authenticated user.
/// The UserId is supplied by the Api layer from the JWT, never from the request body.
/// </summary>
public sealed record GetProfileQuery(int UserId) : IRequest<ProfileResponse?>;
