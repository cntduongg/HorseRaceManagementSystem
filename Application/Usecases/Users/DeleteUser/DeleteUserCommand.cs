using MediatR;

namespace Application.Usecases.Users.DeleteUser;

public sealed record DeleteUserCommand(
    int UserId
) : IRequest<bool>;