using System.Net.Http.Json;
using System.Text.Json;

namespace TitleDocNormalizer.Cli.Services;

public sealed class OllamaClient(string baseUrl)
{
    public async Task<string> GenerateAsync(string model, string prompt, int timeoutSeconds, CancellationToken cancellationToken = default)
    {
        using var http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(timeoutSeconds)
        };

        var request = new
        {
            model,
            prompt,
            stream = false,
            options = new
            {
                temperature = 0,
                num_ctx = 8192
            }
        };

        using var response = await http.PostAsJsonAsync("api/generate", request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(body);
        if (doc.RootElement.TryGetProperty("response", out var responseElement))
        {
            return responseElement.GetString() ?? string.Empty;
        }

        return body;
    }
}
