using MediatR;

namespace Application.Common;

public interface ICommand : IRequest;

public interface ICommand<out TResponse> : IRequest<TResponse>;