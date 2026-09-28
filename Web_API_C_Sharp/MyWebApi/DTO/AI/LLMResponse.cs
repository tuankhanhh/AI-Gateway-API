namespace MyWebApi.DTO.AI
{
    public class LLMResponse
    {
        public string Content { get; set; } = string.Empty;
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
    }
}
