using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using DoacaoApp.Application.CollectionPoints.Contracts;

namespace DoacaoApp.Api.Seo;

/// <summary>Conteúdo das páginas públicas renderizadas no servidor e do sitemap.</summary>
public static class SeoPages
{
    public const string SiteName = "Dooa";
    public const int MaxDescriptionLength = 160;

    private const string HomeTitle = "Dooa — Pontos de Doação e Coleta";
    private const string HomeDescription =
        "Encontre pontos de doação e coleta perto de você e acompanhe retiradas com equidade entre beneficiários no seu município.";

    private static readonly JsonSerializerOptions JsonLdOptions = new() { WriteIndented = false };

    private static readonly Dictionary<string, string> PointTypeLabels = new()
    {
        ["Association"] = "Associação",
        ["Church"] = "Igreja",
        ["PublicEntity"] = "Órgão público",
    };

    public static string PointPath(CollectionPointResponse point) => $"/pontos/{point.Id}/{point.Slug}";

    public static SeoPage Home(IReadOnlyList<CollectionPointResponse> points, string siteUrl)
    {
        var noscript = new StringBuilder()
            .Append("<main><h1>Dooa — pontos de doação e coleta</h1>")
            .Append($"<p>{SeoHtmlRenderer.Encode(HomeDescription)}</p>");

        if (points.Count > 0)
        {
            noscript.Append("<h2>Pontos de coleta</h2><ul>");
            foreach (var point in points)
            {
                noscript.Append(
                    $"<li><a href=\"{PointPath(point)}\">{SeoHtmlRenderer.Encode(point.Name)}</a> — " +
                    $"{SeoHtmlRenderer.Encode(point.Address.City)}/{SeoHtmlRenderer.Encode(point.Address.State)}</li>");
            }

            noscript.Append("</ul>");
        }

        noscript.Append("</main>");

        var itemList = new
        {
            context = "https://schema.org",
            type = "ItemList",
            name = "Pontos de doação e coleta",
            itemListElement = points.Select((point, index) => new
            {
                type = "ListItem",
                position = index + 1,
                url = siteUrl + PointPath(point),
                name = point.Name,
            }),
        };

        return new SeoPage(
            HomeTitle,
            HomeDescription,
            $"{siteUrl}/",
            ImageUrl: null,
            ImageAlt: HomeTitle,
            JsonLd: points.Count > 0 ? ToJsonLd(itemList) : null,
            NoscriptHtml: noscript.ToString());
    }

    public static SeoPage Point(CollectionPointResponse point, string siteUrl)
    {
        var place = $"{point.Address.City}/{point.Address.State}";
        var title = $"{point.Name} — ponto de doação em {place} | {SiteName}";
        var canonicalUrl = siteUrl + PointPath(point);
        var address = FormatAddress(point.Address);

        var noscript = new StringBuilder()
            .Append($"<main><h1>{SeoHtmlRenderer.Encode(point.Name)}</h1>")
            .Append($"<p>{SeoHtmlRenderer.Encode(PointTypeLabel(point))} — ponto de doação e coleta em {SeoHtmlRenderer.Encode(place)}.</p>");

        if (!string.IsNullOrWhiteSpace(point.Description))
        {
            noscript.Append($"<p>{SeoHtmlRenderer.Encode(point.Description)}</p>");
        }

        noscript.Append($"<p>Endereço: {SeoHtmlRenderer.Encode(address)}</p>");

        if (!string.IsNullOrWhiteSpace(point.OpeningHours))
        {
            noscript.Append($"<p>Horário: {SeoHtmlRenderer.Encode(point.OpeningHours)}</p>");
        }

        if (point.Needs.Count > 0)
        {
            noscript.Append("<h2>Precisa agora</h2><ul>");
            foreach (var need in point.Needs)
            {
                noscript.Append($"<li>{SeoHtmlRenderer.Encode(need)}</li>");
            }

            noscript.Append("</ul>");
        }

        noscript.Append($"<p>Contato: {SeoHtmlRenderer.Encode(point.Contact.Phone)}</p>")
            .Append("<p><a href=\"/\">Ver todos os pontos de coleta</a></p></main>");

        var imageUrl = point.ImageUrl is null ? null : $"{siteUrl}/api/public/collection-points/{point.Id}/image";

        var jsonLd = new
        {
            context = "https://schema.org",
            type = "Place",
            name = point.Name,
            description = PointDescription(point),
            url = canonicalUrl,
            image = imageUrl,
            telephone = point.Contact.Phone,
            address = new
            {
                type = "PostalAddress",
                streetAddress = $"{point.Address.Street}, {point.Address.Number}",
                addressLocality = point.Address.City,
                addressRegion = point.Address.State,
                postalCode = point.Address.ZipCode,
                addressCountry = "BR",
            },
            geo = point.Address is { Latitude: { } latitude, Longitude: { } longitude }
                ? new { type = "GeoCoordinates", latitude, longitude }
                : null,
        };

        return new SeoPage(
            title,
            PointDescription(point),
            canonicalUrl,
            imageUrl,
            ImageAlt: $"Foto de {point.Name}",
            JsonLd: ToJsonLd(jsonLd),
            NoscriptHtml: noscript.ToString());
    }

    public static SeoPage NotFound(string siteUrl) =>
        new(
            $"Página não encontrada | {SiteName}",
            "A página procurada não existe ou foi removida. Veja os pontos de doação e coleta no mapa do Dooa.",
            $"{siteUrl}/",
            ImageUrl: null,
            ImageAlt: HomeTitle,
            JsonLd: null,
            NoscriptHtml: "<main><h1>Página não encontrada</h1><p><a href=\"/\">Ver os pontos de doação e coleta</a></p></main>",
            Indexable: false);

    public static string Sitemap(IReadOnlyList<CollectionPointResponse> points, string siteUrl)
    {
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var home = new XElement(ns + "url",
            new XElement(ns + "loc", $"{siteUrl}/"),
            new XElement(ns + "changefreq", "daily"),
            new XElement(ns + "priority", "1.0"));
        var pointUrls = points.Select(point => new XElement(ns + "url",
            new XElement(ns + "loc", siteUrl + PointPath(point)),
            new XElement(ns + "changefreq", "weekly"),
            new XElement(ns + "priority", "0.8")));

        var document = new XDocument(new XDeclaration("1.0", "UTF-8", null), new XElement(ns + "urlset", home, pointUrls));
        return document.Declaration + "\n" + document.Root;
    }

    public static string PointDescription(CollectionPointResponse point)
    {
        var parts = new List<string>();
        parts.Add(string.IsNullOrWhiteSpace(point.Description)
            ? $"Ponto de doação e coleta em {point.Address.City}/{point.Address.State}."
            : EnsureSentence(point.Description.Trim()));
        parts.Add($"Endereço: {FormatAddress(point.Address)}.");

        if (point.Needs.Count > 0)
        {
            parts.Add($"Precisa agora: {string.Join(", ", point.Needs)}.");
        }

        return Truncate(string.Join(' ', parts), MaxDescriptionLength);
    }

    private static string PointTypeLabel(CollectionPointResponse point) =>
        PointTypeLabels.GetValueOrDefault(point.Type, "Ponto de coleta");

    private static string FormatAddress(AddressResponse address) =>
        $"{address.Street}, {address.Number} - {address.District}, {address.City}/{address.State}";

    private static string EnsureSentence(string text) =>
        text.EndsWith('.') || text.EndsWith('!') || text.EndsWith('?') ? text : text + ".";

    private static string Truncate(string text, int maxLength)
    {
        if (text.Length <= maxLength)
        {
            return text;
        }

        var cut = text[..(maxLength - 1)];
        var lastSpace = cut.LastIndexOf(' ');
        return (lastSpace > maxLength / 2 ? cut[..lastSpace] : cut).TrimEnd(',', '.', ' ', '-') + "…";
    }

    /// <summary>Serializa com "@context"/"@type" (nomes que C# não aceita como propriedade) e sem campos nulos.</summary>
    private static string ToJsonLd(object value)
    {
        var node = JsonSerializer.SerializeToNode(value, JsonLdOptions)!;
        return RenameSchemaKeys(node)!.ToJsonString(JsonLdOptions);
    }

    private static JsonNode? RenameSchemaKeys(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                var renamed = new JsonObject();
                foreach (var (key, child) in obj.ToList())
                {
                    if (child is null)
                    {
                        continue;
                    }

                    obj.Remove(key);
                    renamed[key is "context" or "type" ? "@" + key : key] = RenameSchemaKeys(child);
                }

                return renamed;
            case JsonArray array:
                var items = array.ToList();
                array.Clear();
                return new JsonArray([.. items.Select(RenameSchemaKeys)]);
            default:
                return node;
        }
    }
}
