using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MyWebApi.DTO.AI;
using MyWebApi.Providers.Interfaces;

namespace MyWebApi.Providers.OpenAI
{
    public class OpenAIProvider : ILLMProvider
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _baseUrl;

        public OpenAIProvider(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["OpenAI:ApiKey"] ?? string.Empty;
            _baseUrl = configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1";

            if (!string.IsNullOrEmpty(_apiKey))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            }
        }

        public async Task<LLMResponse> ChatAsync(LLMRequest request)
        {
            var openAiRequest = new
            {
                model = request.Model,
                messages = request.Messages
            };

            var jsonContent = JsonSerializer.Serialize(openAiRequest, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/chat/completions", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                throw new Exception($"OpenAI API error: {response.StatusCode} - {errorResponse}");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonResponse);

            var root = doc.RootElement;
            var messageContent = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
            
            int inputTokens = 0;
            int outputTokens = 0;

            if (root.TryGetProperty("usage", out var usageProp))
            {
                if (usageProp.TryGetProperty("prompt_tokens", out var promptTokensProp))
                {
                    inputTokens = promptTokensProp.GetInt32();
                }
                
                if (usageProp.TryGetProperty("completion_tokens", out var completionTokensProp))
                {
                    outputTokens = completionTokensProp.GetInt32();
                }
            }

            return new LLMResponse
            {
                Content = messageContent,
                InputTokens = inputTokens,
                OutputTokens = outputTokens
            };
        }
    }
}
