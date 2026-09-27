using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MyWebApi.Models;

[Index("UserId", Name = "IX_Conversations_UserId")]
[Index("UserId", "UpdatedAt", Name = "IX_Conversations_UserId_UpdatedAt")]
public partial class Conversation
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    [StringLength(200)]
    public string? Title { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [InverseProperty("Conversation")]
    public virtual ICollection<AiRequestLog> AiRequestLogs { get; set; } = new List<AiRequestLog>();

    [InverseProperty("Conversation")]
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    [ForeignKey("UserId")]
    [InverseProperty("Conversations")]
    public virtual User User { get; set; } = null!;
}
