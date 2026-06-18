using Application.Common;
using MediatR;

namespace Application.Usecases.Auth.RefreshToken;

public sealed record RefreshTokenCommand(string RefreshToken) : ICommand<RefreshTokenResponse>;
