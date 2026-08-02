using System.ComponentModel.DataAnnotations;
using Xenoh.Domain.Common;
using Xenoh.Domain.Enums;

namespace Xenoh.Domain.Entities;

public sealed class LegalAcceptance : BaseEntity
{
    public Guid UserId { get; set; }
    public LegalDocumentType DocumentType { get; set; }
    [MaxLength(40)] public string DocumentVersion { get; set; } = string.Empty;
    public DateTime AcceptedAt { get; set; }
    public Guid PaymentOrderId { get; set; }
}
