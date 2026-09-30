using System.ComponentModel.DataAnnotations;

namespace MyWebApi.DTO.AI
{
    public class AnalyzeRequestDto
    {
        [Required(AllowEmptyStrings = false, ErrorMessage = "Text is required")]
        public string Text { get; set; } = string.Empty;
    }
}
