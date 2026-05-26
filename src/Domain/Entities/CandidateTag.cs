namespace RC.HyRe.Domain.Entities;

public class CandidateTag
{
    public Guid CandidateId { get; set; }
    public Guid TagId { get; set; }
    public DateTimeOffset AssignedAt { get; set; }
    public string? AssignedBy { get; set; }  // user ID

    // Navigation properties
    public Candidate Candidate { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}
