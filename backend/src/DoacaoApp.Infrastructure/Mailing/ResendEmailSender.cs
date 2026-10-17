using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace DoacaoApp.Infrastructure.Mailing;

/// <summary>Envia e-mails pela API HTTP do Resend (https://resend.com/docs/api-reference/emails/send-email).</summary>
public sealed class ResendEmailSender(HttpClient httpClient, IOptions<EmailOptions> options) : IEmailSender
{
    public static readonly Uri ApiBaseAddress = new("https://api.resend.com/");

    private readonly EmailOptions _options = options.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "emails")
        {
            Content = JsonContent.Create(new
            {
                from = _options.FromAddress,
                to = new[] { message.To },
                subject = message.Subject,
                html = message.HtmlBody,
                text = message.TextBody,
            }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ResendApiKey);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Resend recusou o e-mail ({(int)response.StatusCode}): {detail}", null, response.StatusCode);
        }
    }
}
