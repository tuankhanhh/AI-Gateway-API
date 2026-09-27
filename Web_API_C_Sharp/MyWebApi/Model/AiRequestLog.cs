using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MyWebApi.Models;

[Index("ConversationId", Name = "IX_AiRequestLogs_ConversationId")]
[Index("Model", "RequestedAt", Name = "IX_AiRequestLogs_Model_RequestedAt")]
[Index("Status", "RequestedAt", Name = "IX_AiRequestLogs_Status_RequestedAt")]
[Index("UserId", "RequestedAt", Name = "IX_AiRequestLogs_UserId_RequestedAt")]
public partial class AiRequestLog
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    public int? ConversationId { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string Provider { get; set; } = null!;

    [StringLength(100)]
    [Unicode(false)]
    public string Model { get; set; } = null!;

    public DateTime RequestedAt { get; set; }

    public long? LatencyMs { get; set; }

    public int? InputTokens { get; set; }

    public int? OutputTokens { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string Status { get; set; } = null!;

    [StringLength(100)]
    [Unicode(false)]
    public string? ErrorCode { get; set; }

    [ForeignKey("ConversationId")]
    [InverseProperty("AiRequestLogs")]
    public virtual Conversation? Conversation { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("AiRequestLogs")]
    public virtual User User { get; set; } = null!;
}
