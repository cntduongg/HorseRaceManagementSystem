namespace Domain.Aggregates.Entities;

// Nháp kết quả leg của từng referee (Flow 4). KHÁC LegRefereeEntry (append-only):
// draft được GHI ĐÈ mỗi lần lưu (upsert theo Race+Leg+Referee). Đây chỉ là bản nháp
// trước khi submit; bản submit chính thức, chốt, nằm ở LegRefereeEntry.
public class LegRefereeDraft
{
    public long LegRefereeDraftId { get; set; }
    public int RaceId { get; set; }
    public int LegNumber { get; set; }
    public int RefereeUserId { get; set; }
    public int EntryId { get; set; }
    // Position-code theo quy ước FE: >0 = thứ hạng; -1 = DNF; -2 = DQ; 0 = chưa nhập.
    public int Position { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Leg Leg { get; set; } = null!;
    public Entry Entry { get; set; } = null!;
    public User Referee { get; set; } = null!;
}
