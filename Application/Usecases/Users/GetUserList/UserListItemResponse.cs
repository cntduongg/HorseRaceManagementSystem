namespace Application.Usecases.Users.GetUserList;

public sealed record UserListItemResponse(
	int UserId,
	string Email,
	string FullName,
	string? PhoneNumber,
	string? AvatarUrl,
	int RoleId,
	string? RoleCode,
	bool IsActive,
	string Status,
	DateTime? LockedUntil,
	string? LicenseNumber,
	decimal? Weight,
	string? Bio,
	bool IsProfileComplete,
	DateTime CreatedAt
);

// Bọc kết quả phân trang cho GET /api/users (FE kỳ vọng { items, total, page, pageSize }).
public sealed record PagedUserListResponse(
	IReadOnlyList<UserListItemResponse> Items,
	int Total,
	int Page,
	int PageSize
);
