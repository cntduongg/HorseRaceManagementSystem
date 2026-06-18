using Application.Common;
using MediatR;

namespace Application.Usecases.Auth.Logout;

public sealed record LogoutCommand(string RefreshToken) : ICommand<bool>;
