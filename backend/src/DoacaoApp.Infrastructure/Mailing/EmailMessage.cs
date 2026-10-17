namespace DoacaoApp.Infrastructure.Mailing;

/// <summary>Um e-mail pronto para envio, com versões HTML e texto puro do corpo.</summary>
public sealed record EmailMessage(string To, string Subject, string HtmlBody, string TextBody);
