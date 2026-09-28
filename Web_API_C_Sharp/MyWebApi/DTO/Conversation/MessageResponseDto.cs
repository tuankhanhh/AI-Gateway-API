using System;

namespace MyWebApi.DTO.Conversation
{
    public class MessageResponseDto
    {
        public int Id { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Model { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
