using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MyWebApi.Models;

[Index("FamilyId", Name = "IX_RefreshTokens_FamilyId")]
[Index("TokenHash", Name = "IX_RefreshTokens_TokenHash", IsUnique = true)]
[Index("UserId", Name = "IX_RefreshTokens_UserId")]
[Index("TokenHash", Name = "UQ__RefreshT__BCB33F92A03DF68A", IsUnique = true)]
public partial class RefreshToken
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    [StringLength(500)]
    [Unicode(false)]
    public string TokenHash { get; set; } = null!;

    public Guid FamilyId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("RefreshTokens")]
    public virtual User User { get; set; } = null!;
}
