using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Text.Unicode;

namespace DoacaoApp.Api.Seo;

/// <summary>
/// Aplica uma <see cref="SeoPage"/> sobre o index.html do frontend: título, descrição, canonical, robots, Open
/// Graph/Twitter, JSON-LD e um bloco &lt;noscript&gt; com o conteúdo para quem não executa JavaScript. O app React
/// continua sendo carregado normalmente.
/// </summary>
public static partial class SeoHtmlRenderer
{
    private const string RootElement = "<div id=\"root\"></div>";
    private const string IndexableRobots = "index, follow, max-image-preview:large";
    private const string NotIndexableRobots = "noindex, follow";

    private static readonly HtmlEncoder HtmlTextEncoder = HtmlEncoder.Create(UnicodeRanges.All);

    private static readonly string[] DefaultImageSizeMetas = ["og:image:type", "og:image:width", "og:image:height"];

    public static string Render(string template, SeoPage page)
    {
        var html = TitleTag().Replace(template, $"<title>{Encode(page.Title)}</title>", 1);
        html = CanonicalTag().Replace(html, $"<link rel=\"canonical\" href=\"{Encode(page.CanonicalUrl)}\" />", 1);

        html = SetMeta(html, "description", page.Description);
        html = SetMeta(html, "robots", page.Indexable ? IndexableRobots : NotIndexableRobots);
        html = SetMeta(html, "og:url", page.CanonicalUrl);
        html = SetMeta(html, "og:title", page.Title);
        html = SetMeta(html, "og:description", page.Description);
        html = SetMeta(html, "twitter:title", page.Title);
        html = SetMeta(html, "twitter:description", page.Description);

        if (page.ImageUrl is { } imageUrl)
        {
            html = SetMeta(html, "og:image", imageUrl);
            html = SetMeta(html, "og:image:secure_url", imageUrl);
            html = SetMeta(html, "twitter:image", imageUrl);
            html = DefaultImageSizeMetas.Aggregate(html, RemoveMeta);
        }

        html = SetMeta(html, "og:image:alt", page.ImageAlt);
        html = SetMeta(html, "twitter:image:alt", page.ImageAlt);

        if (page.JsonLd is { } jsonLd)
        {
            html = html.Replace("</head>", $"<script type=\"application/ld+json\">{jsonLd}</script>\n  </head>", StringComparison.Ordinal);
        }

        return html.Replace(RootElement, $"<noscript>{page.NoscriptHtml}</noscript>\n    {RootElement}", StringComparison.Ordinal);
    }

    /// <summary>Escapa só o que é perigoso em HTML (&lt;, &gt;, &amp;, aspas); acentos ficam legíveis.</summary>
    public static string Encode(string text) => HtmlTextEncoder.Encode(text);

    private static string SetMeta(string html, string key, string value) =>
        MetaTag(key).Replace(html, match => $"{match.Groups["start"].Value}{Encode(value)}{match.Groups["end"].Value}", 1);

    private static string RemoveMeta(string html, string key) =>
        new Regex($@"[ \t]*<meta\s+(?:name|property)=""{Regex.Escape(key)}""\s+content=""[^""]*""\s*/?>\r?\n?").Replace(html, string.Empty, 1);

    private static Regex MetaTag(string key) =>
        new($@"(?<start><meta\s+(?:name|property)=""{Regex.Escape(key)}""\s+content="")[^""]*(?<end>""\s*/?>)");

    [GeneratedRegex(@"<title>.*?</title>", RegexOptions.Singleline)]
    private static partial Regex TitleTag();

    [GeneratedRegex(@"<link\s+rel=""canonical""\s+href=""[^""]*""\s*/?>")]
    private static partial Regex CanonicalTag();
}
