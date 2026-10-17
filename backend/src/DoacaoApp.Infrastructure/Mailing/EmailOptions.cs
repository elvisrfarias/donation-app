namespace DoacaoApp.Infrastructure.Mailing;

/// <summary>Envio de e-mails transacionais pelo Resend, seção "Email".</summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>Chave da API do Resend. Vazia, os e-mails não são enviados e só aparecem no log.</summary>
    public string? ResendApiKey { get; init; }

    /// <summary>Remetente, num domínio verificado no Resend (ex.: "Dooa &lt;nao-responda@dooa.com.br&gt;").</summary>
    public string FromAddress { get; init; } = "Dooa <onboarding@resend.dev>";

    /// <summary>Endereço público do frontend, base dos links enviados por e-mail.</summary>
    public string AppBaseUrl { get; init; } = "http://localhost:5173";

    public bool IsResendConfigured => !string.IsNullOrWhiteSpace(ResendApiKey);
}
