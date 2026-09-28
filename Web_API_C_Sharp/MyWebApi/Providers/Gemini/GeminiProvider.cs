using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MyWebApi.DTO.AI;
using MyWebApi.Exceptions;
using MyWebApi.Providers.Interfaces;

namespace MyWebApi.Providers.Gemini
{
    public class GeminiProvider : ILLMProvider
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _baseUrl;

        public GeminiProvider(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Gemini:ApiKey"] ?? string.Empty;
            _baseUrl = configuration["Gemini:BaseUrl"] ?? "https://generativelanguage.googleapis.com/v1beta/models";
        }

        public async Task<LLMResponse> ChatAsync(LLMRequest request)
        {
            var model = string.IsNullOrEmpty(request.Model) ? "gemini-1.5-pro" : request.Model;
            
            var contents = request.Messages.Select(m => new
            {
                role = m.Role == "assistant" ? "model" : "user",
                parts = new[] { new { text = m.Content } }
            }).ToList();

            var geminiRequest = new
            {
                contents = contents
            };

            var jsonContent = JsonSerializer.Serialize(geminiRequest, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var requestUrl = $"{_baseUrl}/{model}:generateContent?key={_apiKey}";
            
            var response = await SendRequestWithRetryAsync(requestUrl, jsonContent);
            var jsonResponse = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonResponse);

            var root = doc.RootElement;
            var candidates = root.GetProperty("candidates");
            var messageContent = string.Empty;

            if (candidates.GetArrayLength() > 0)
            {
                var firstCandidate = candidates[0];
                var contentProp = firstCandidate.GetProperty("content");
                var parts = contentProp.GetProperty("parts");
                if (parts.GetArrayLength() > 0)
                {
                    messageContent = parts[0].GetProperty("text").GetString() ?? string.Empty;
                }
            }
            
            int inputTokens = 0;
            int outputTokens = 0;

            if (root.TryGetProperty("usageMetadata", out var usageProp))
            {
                if (usageProp.TryGetProperty("promptTokenCount", out var promptTokensProp))
                {
                    inputTokens = promptTokensProp.GetInt32();
                }
                
                if (usageProp.TryGetProperty("candidatesTokenCount", out var completionTokensProp))
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

        private async Task<HttpResponseMessage> SendRequestWithRetryAsync(string url, string jsonContent)
        {
            int maxRetries = 3;
            int[] retryDelays = { 500, 1000, 2000 };
            
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            
            for (int i = 0; i <= maxRetries; i++)
            {
                try
                {
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync(url, content, cts.Token);

                    if (!response.IsSuccessStatusCode)
                    {
                        int statusCode = (int)response.StatusCode;
                        bool isTransientError = statusCode == 429 || statusCode == 500 || statusCode == 502 || statusCode == 503 || statusCode == 504;

                        if (isTransientError && i < maxRetries)
                        {
                            await Task.Delay(retryDelays[i]);
                            continue;
                        }
                        
                        MapAndThrowProviderError(statusCode, await response.Content.ReadAsStringAsync());
                    }

                    return response;
                }
                catch (TaskCanceledException) when (cts.IsCancellationRequested)
                {
                    if (i < maxRetries)
                    {
                        await Task.Delay(retryDelays[i]);
                        continue;
                    }
                    throw new AiProviderException(504, "PROVIDER_TIMEOUT", "AI provider request timed out.");
                }
                catch (HttpRequestException ex)
                {
                    if (i < maxRetries)
                    {
                        await Task.Delay(retryDelays[i]);
                        continue;
                    }
                    throw new AiProviderException(502, "PROVIDER_UNAVAILABLE", $"Network error: {ex.Message}");
                }
            }
            
            throw new AiProviderException(502, "PROVIDER_UNAVAILABLE", "AI provider is temporarily unavailable after retries.");
        }

        private void MapAndThrowProviderError(int statusCode, string errorContent)
        {
            switch (statusCode)
            {
                case 400:
                    throw new AiProviderException(400, "PROVIDER_BAD_REQUEST", $"Bad request to provider: {errorContent}");
                case 401:
                    throw new AiProviderException(401, "PROVIDER_UNAUTHORIZED", "Provider unauthorized.");
                case 403:
                    throw new AiProviderException(403, "PROVIDER_FORBIDDEN", "Provider forbidden.");
                case 404:
                    throw new AiProviderException(404, "PROVIDER_NOT_FOUND", "Provider resource not found.");
                case 429:
                    throw new AiProviderException(429, "PROVIDER_RATE_LIMITED", "Provider rate limit exceeded.");
                case 500:
                    throw new AiProviderException(500, "PROVIDER_INTERNAL_ERROR", "Provider internal error.");
                case 503:
                case 502:
                default:
                    throw new AiProviderException(502, "PROVIDER_UNAVAILABLE", $"Provider error {statusCode}: {errorContent}");
            }
        }
    }
}
