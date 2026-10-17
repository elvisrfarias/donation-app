using System.Net;
using DoacaoApp.Application.Common.Abstractions;

namespace DoacaoApp.Infrastructure.Mailing;

/// <summary>Conteúdo do e-mail de "esqueci minha senha".</summary>
public static class PasswordResetEmail
{
    public const string Subject = "Redefinição de senha do Dooa";

    public static EmailMessage Build(PasswordResetTicket ticket, Uri resetLink, int validityMinutes)
    {
        var firstName = FirstNameOf(ticket.FullName);
        var greeting = string.IsNullOrEmpty(firstName) ? "Olá!" : $"Olá, {firstName}!";

        var text = $"""
            {greeting}

            Recebemos um pedido para redefinir a senha da sua conta no Dooa.
            Para escolher uma nova senha, abra o link abaixo (ele vale por {validityMinutes} minutos e só pode ser usado uma vez):

            {resetLink}

            Se não foi você quem pediu, ignore este e-mail: sua senha atual continua valendo.
            """;

        var html = $"""
            <!doctype html>
            <html lang="pt-BR">
            <body style="margin:0;background:#f5f5f4;font-family:Arial,Helvetica,sans-serif;color:#1c1917">
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="padding:32px 16px">
                <tr><td align="center">
                  <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:480px;background:#ffffff;border-radius:16px;padding:32px">
                    <tr><td>
                      <p style="margin:0 0 24px;font-size:22px;font-weight:bold;color:#2563eb">Dooa</p>
                      <p style="margin:0 0 16px;font-size:16px">{WebUtility.HtmlEncode(greeting)}</p>
                      <p style="margin:0 0 24px;font-size:15px;line-height:1.5">Recebemos um pedido para redefinir a senha da sua conta. Clique no botão para escolher uma nova senha.</p>
                      <p style="margin:0 0 24px"><a href="{WebUtility.HtmlEncode(resetLink.AbsoluteUri)}" style="display:inline-block;background:#2563eb;color:#ffffff;text-decoration:none;font-weight:bold;font-size:15px;padding:12px 24px;border-radius:12px">Redefinir minha senha</a></p>
                      <p style="margin:0 0 16px;font-size:13px;line-height:1.5;color:#57534e">O link vale por {validityMinutes} minutos e só pode ser usado uma vez.</p>
                      <p style="margin:0;font-size:13px;line-height:1.5;color:#57534e">Se não foi você quem pediu, ignore este e-mail: sua senha atual continua valendo.</p>
                    </td></tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;

        return new EmailMessage(ticket.Email, Subject, html, text);
    }

    private static string FirstNameOf(string fullName) =>
        fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? string.Empty;
}
