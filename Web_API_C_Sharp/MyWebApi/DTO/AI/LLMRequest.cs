using System.Collections.Generic;

namespace MyWebApi.DTO.AI
{
    public class LLMRequest
    {
        public string Model { get; set; } = string.Empty;
        public List<LLMMessage> Messages { get; set; } = new();
    }

    public class LLMMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
