namespace MyWebApi.DTO.Usage
{
    public class UsageResponseDto
    {
        public int Requests { get; set; }
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
        public int TotalTokens { get; set; }
        public double AverageLatencyMs { get; set; }
        public double ErrorRate { get; set; }
    }
}
