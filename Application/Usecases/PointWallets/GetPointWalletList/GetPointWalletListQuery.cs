using MediatR;

namespace Application.Usecases.PointWallets.GetPointWalletList;

public sealed record GetPointWalletListQuery()
    : IRequest<List<PointWalletListItemResponse>>;