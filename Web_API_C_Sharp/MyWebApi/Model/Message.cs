using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MyWebApi.Models;

[Index("ConversationId", "CreatedAt", Name = "IX_Messages_ConversationId_CreatedAt")]
public partial class Message
{
    [Key]
    public int Id { get; set; }

    public int ConversationId { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string Role { get; set; } = null!;

    public string Content { get; set; } = null!;

    [StringLength(100)]
    [Unicode(false)]
    public string? Model { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey("ConversationId")]
    [InverseProperty("Messages")]
    public virtual Conversation Conversation { get; set; } = null!;
}
