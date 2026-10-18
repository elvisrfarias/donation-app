using System.Globalization;
using System.Text;

namespace DoacaoApp.Application.Common.Text;

/// <summary>
/// Trecho legível de URL ("casa-solidaria-concordia-sc"): minúsculas, sem acentos, só letras, dígitos e hífens.
/// Ajuda o Google e quem recebe o link a entender a página antes de abrir.
/// </summary>
public static class UrlSlug
{
    public const int MaxLength = 80;

    public static string From(params string?[] parts)
    {
        var text = string.Join(' ', parts.Where(part => !string.IsNullOrWhiteSpace(part)));
        var builder = new StringBuilder(text.Length);
        var pendingSeparator = false;

        foreach (var character in RemoveDiacritics(text).ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(character))
            {
                if (pendingSeparator && builder.Length > 0)
                {
                    builder.Append('-');
                }

                builder.Append(character);
                pendingSeparator = false;
            }
            else
            {
                pendingSeparator = true;
            }
        }

        return TrimToMaxLength(builder.ToString());
    }

    private static string RemoveDiacritics(string text)
    {
        var decomposed = text.Normalize(NormalizationForm.FormD);
        var withoutMarks = decomposed.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
        return new string([.. withoutMarks]).Normalize(NormalizationForm.FormC);
    }

    private static string TrimToMaxLength(string slug) =>
        slug.Length <= MaxLength ? slug : slug[..MaxLength].TrimEnd('-');
}
