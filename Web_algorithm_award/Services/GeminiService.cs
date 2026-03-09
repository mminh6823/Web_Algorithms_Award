using System.Text;
using System.Text.Json;

namespace Web_algorithm_award.Services;

public class GeminiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public GeminiService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _apiKey = config["Gemini:ApiKey"]
            ?? throw new Exception("Gemini API key not configured.");
    }

    public async Task<string> GenerateExplanation(string prompt)
    {
        var url =
            $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";

        var body = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(body);

        int maxRetry = 5;

        for (int i = 0; i < maxRetry; i++)
        {
            try
            {
                var response = await _httpClient.PostAsync(
                    url,
                    new StringContent(json, Encoding.UTF8, "application/json")
                );

                var result = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    await Task.Delay(3000);
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    return $"Gemini API error: {result}";
                }

                using var doc = JsonDocument.Parse(result);

                if (!doc.RootElement.TryGetProperty("candidates", out var candidates))
                    return "Gemini returned unexpected response.";

                var text = candidates[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return text ?? "Gemini returned empty response.";
            }
            catch (HttpRequestException ex)
            {
                if (i == maxRetry - 1)
                    return $"Network error calling Gemini: {ex.Message}";

                await Task.Delay(2000);
            }
            catch (Exception ex)
            {
                return $"Unexpected error: {ex.Message}";
            }
        }

        return "Gemini rate limit exceeded. Please try again later.";
    }
}