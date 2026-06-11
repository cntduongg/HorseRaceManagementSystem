using Application.Common;
using MediatR;

namespace Application.Usecases.Auth.Login;

public sealed record LoginCommand(string Email, string Password) : ICommand<LoginResponse>;
