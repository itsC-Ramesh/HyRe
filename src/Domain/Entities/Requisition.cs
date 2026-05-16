using RC.HyRe.Domain.Common;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Domain.Entities;

/// <summary>
/// An open job role. The starting point for every hiring workflow.
/// OwnerId references the hiring manager (AspNetUsers.Id).
/// </summary>
public class Requisition : HiringBaseEntity
{
    public required string Title { get; set; }

    public required string Department { get; set; }

    /// <summary>FK → AspNetUsers (string, matches IdentityUser.Id).</summary>
    public required string OwnerId { get; set; }

    public required string JdText { get; set; }

    public int? SalaryMin { get; set; }

    public int? SalaryMax { get; set; }

    public int Headcount { get; set; } = 1;

    public RequisitionStatus Status { get; set; } = RequisitionStatus.Draft;

    // Navigation
    public ICollection<JobApplication> Applications { get; set; } = [];
}
