using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ContactLandingApi.Services;

public class ResendEmailService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public ResendEmailService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<(bool ok, string? id, string? error)> SendRegistrationEmailAsync(
        string toEmail,
        string nombre,
        string codigo,
        CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["Resend:ApiKey"];
       
        if (string.IsNullOrWhiteSpace(apiKey))
            return (false, null, "Falta Resend:ApiKey");

        if (string.IsNullOrWhiteSpace(from))
            return (false, null, "Falta Resend:From");

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var payload = new
        {
            to = new[] { toEmail },
            template = new
            {
                id = "tuwaexpo10-1",
                variables = new
                {
                    nombre,
                    codigo
                }
            }
        };

        using var response = await client.PostAsJsonAsync(
            "https://api.resend.com/emails",
            payload,
            cancellationToken);

        var body = await response.Content.ReadFromJsonAsync<ResendResponse>(cancellationToken: cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return (false, null, body?.Message ?? $"Error HTTP {(int)response.StatusCode}");
        }

        return (true, body?.Id, null);
    }

    private sealed class ResendResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
