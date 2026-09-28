using System.ComponentModel.DataAnnotations;

namespace MyWebApi.DTO.Conversation
{
    public class MessageCreateDto
    {
        [Required]
        public string Content { get; set; } = string.Empty;
    }
}
