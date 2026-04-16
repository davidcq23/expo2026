using System.Net.Http.Json;

namespace ContactLandingApi.Services;

public class CaptchaValidationService(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<CaptchaValidationService> logger)
{
    public async Task<bool> ValidateAsync(string? token, CancellationToken cancellationToken = default)
    {
        var provider = configuration["Captcha:Provider"]?.Trim();
        if (string.IsNullOrWhiteSpace(provider) || provider.Equals("None", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var secretKey = configuration["Captcha:SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            logger.LogError("Captcha configurado pero falta Captcha:SecretKey");
            return false;
        }

        if (provider.Equals("Turnstile", StringComparison.OrdinalIgnoreCase))
        {
            var client = httpClientFactory.CreateClient();
            var response = await client.PostAsync(
                "https://challenges.cloudflare.com/turnstile/v0/siteverify",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["secret"] = secretKey,
                    ["response"] = token
                }),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Turnstile devolvió HTTP {StatusCode}", response.StatusCode);
                return false;
            }

            var payload = await response.Content.ReadFromJsonAsync<TurnstileResponse>(cancellationToken: cancellationToken);
            return payload?.Success == true;
        }

        logger.LogWarning("Proveedor captcha no soportado: {Provider}", provider);
        return false;
    }

    private sealed class TurnstileResponse
    {
        public bool Success { get; set; }
    }
}
