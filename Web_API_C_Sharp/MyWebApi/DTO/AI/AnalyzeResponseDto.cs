namespace MyWebApi.DTO.AI
{
    public class AnalyzeResponseDto
    {
        public string Sentiment { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
    }
}
