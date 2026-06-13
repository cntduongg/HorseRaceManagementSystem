using MediatR;

namespace Application.Usecases.Entries.GetEntryDetail;

public sealed class GetEntryDetailQueryHandler
	: IRequestHandler<GetEntryDetailQuery, EntryDetailResponse?>
{
	public Task<EntryDetailResponse?> Handle(
		GetEntryDetailQuery request,
		CancellationToken cancellationToken)
	{
		// TODO: Load from database

		var response = new EntryDetailResponse(
			request.EntryId,
			1,
			1,
			2,
			3,
			"Pending",
			null
		);

		return Task.FromResult<EntryDetailResponse?>(response);
	}
}