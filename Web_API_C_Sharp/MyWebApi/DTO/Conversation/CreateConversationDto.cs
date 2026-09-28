using System.ComponentModel.DataAnnotations;

namespace MyWebApi.DTO.Conversation
{
    public class CreateConversationDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
    }
}
