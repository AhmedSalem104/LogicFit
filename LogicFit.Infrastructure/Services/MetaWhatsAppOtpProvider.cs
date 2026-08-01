using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Enums;
using Microsoft.Extensions.Options;

namespace LogicFit.Infrastructure.Services;

public sealed class MetaWhatsAppOtpProvider : IOtpSender
{
    private readonly HttpClient _http;
    private readonly MetaWhatsAppOptions _options;

    public MetaWhatsAppOtpProvider(HttpClient http, IOptions<MetaWhatsAppOptions> options)
        => (_http, _options) = (http, options.Value);

    public async Task<OtpSendResult> SendAsync(string normalizedPhoneNumber, string code, OtpPurpose purpose,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();
        // Retry only explicit 429 rejections, where Meta did not accept the message. Blind retries
        // after network/5xx failures can duplicate OTP messages and are intentionally forbidden.
        for (var attempt = 0; attempt < 3; attempt++)
        {
            using var request = CreateRequest(normalizedPhoneNumber, code);
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.StatusCode == HttpStatusCode.TooManyRequests && attempt < 2)
            {
                var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromMilliseconds(250 * Math.Pow(2, attempt));
                await Task.Delay(retryAfter > TimeSpan.FromSeconds(2) ? TimeSpan.FromSeconds(2) : retryAfter, cancellationToken);
                continue;
            }
            if (response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500)
                throw new HttpRequestException("Meta WhatsApp is temporarily unavailable.", null, response.StatusCode);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var messageId = json.RootElement.TryGetProperty("messages", out var messages) &&
                            messages.GetArrayLength() > 0 &&
                            messages[0].TryGetProperty("id", out var id)
                ? id.GetString()
                : null;
            return new OtpSendResult("MetaWhatsApp", messageId, OtpDeliveryStatus.Sent);
        }
        throw new HttpRequestException("Meta WhatsApp rejected the request due to rate limiting.");
    }

    private HttpRequestMessage CreateRequest(string normalizedPhoneNumber, string code)
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"https://graph.facebook.com/{_options.GraphApiVersion}/{_options.PhoneNumberId}/messages");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);
        request.Content = JsonContent.Create(new
        {
            messaging_product = "whatsapp",
            to = normalizedPhoneNumber.TrimStart('+'),
            type = "template",
            template = new
            {
                name = _options.TemplateName,
                language = new { code = _options.TemplateLanguage },
                components = new object[]
                {
                    new { type = "body", parameters = new[] { new { type = "text", text = code } } },
                    new { type = "button", sub_type = "url", index = "0", parameters = new[] { new { type = "text", text = code } } }
                }
            }
        });
        return request;
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.AccessToken) ||
            string.IsNullOrWhiteSpace(_options.PhoneNumberId) ||
            string.IsNullOrWhiteSpace(_options.TemplateName))
            throw new InvalidOperationException("Meta WhatsApp OTP secrets are not configured.");
    }
}
