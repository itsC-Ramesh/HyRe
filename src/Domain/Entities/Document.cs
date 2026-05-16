using RC.HyRe.Domain.Common;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Domain.Entities;

/// <summary>
/// Metadata record for a file stored in the S3-compatible file store.
/// The binary is in S3; only the FileKey and metadata live here.
/// EntityType + EntityId form a polymorphic association (e.g. 'candidate' + candidate.Id).
/// </summary>
public class Document : HiringBaseEntity
{
    /// <summary>Polymorphic owner type: 'candidate' | 'offer' | 'onboarding'.</summary>
    public required string EntityType { get; set; }

    /// <summary>PK of the owning entity.</summary>
    public Guid EntityId { get; set; }

    /// <summary>S3 object key used to retrieve the binary via the file store.</summary>
    public required string FileKey { get; set; }

    public DocumentType Type { get; set; }

    public required string MimeType { get; set; }

    /// <summary>File size in bytes.</summary>
    public long SizeBytes { get; set; }

    // Back-references (optional navigation — not always loaded)
    public Candidate? CandidateResume { get; set; }
    public Offer? OfferLetter { get; set; }
}
