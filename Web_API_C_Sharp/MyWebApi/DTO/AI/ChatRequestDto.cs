using System.ComponentModel.DataAnnotations;

namespace MyWebApi.DTO.AI
{
    public class ChatRequestDto
    {
        public int? ConversationId { get; set; }

        [Required]
        public string Model { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;
    }
}
