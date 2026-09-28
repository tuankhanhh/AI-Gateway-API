namespace MyWebApi.DTO.AI
{
    public class ChatResponseDto
    {
        public int ConversationId { get; set; }
        public string Model { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public TokenUsageDto Usage { get; set; } = new();
    }

    public class TokenUsageDto
    {
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
        public int TotalTokens { get; set; }
    }
}
