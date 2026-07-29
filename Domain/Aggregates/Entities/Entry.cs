namespace Domain.Aggregates.Entities;

public class Entry
{
    public int EntryId { get; set; }
    public int RaceId { get; set; }
    public int HorseId { get; set; }
    public int JockeyId { get; set; }
    public int HorseOwnerId { get; set; }

    public string Status { get; set; } = "Pending";
    public int? GateNumber { get; set; }
    // ODDS — con số DUY NHẤT: máy tính từ lịch sử thắng lúc đóng đăng ký (Flow 3) rồi đứng
    // yên tới hết race. Đây cũng chính là giá spectator cược và bị khóa vào Prediction, và là
    // hệ số trả thưởng (Flow 7-8). Không ai sửa được. 0 = chưa đóng đăng ký.
    // Nơi duy nhất ghi giá trị này: Application/Usecases/RaceExecution/RaceOddsAssigner.cs
    public decimal Odds { get; set; } = 0;
    public string? RejectionReason { get; set; }

    public DateTime SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public int? ApprovedBy { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }


    public Race Race { get; set; } = null!;
    public Horse Horse { get; set; } = null!;
    public User Jockey { get; set; } = null!;
    public User HorseOwner { get; set; } = null!;
    public ICollection<RaceResult> RaceResults { get; set; }
        = new List<RaceResult>();

    public ICollection<LegRefereeEntry> LegRefereeEntries { get; set; }
        = new List<LegRefereeEntry>();

    public ICollection<LegOfficialResult> LegOfficialResults { get; set; }
        = new List<LegOfficialResult>();

}