using System.Net;
using System.Net.Http.Json;
using AspNetCoreRateLimit;
using DoacaoApp.Api.Seo;
using DoacaoApp.Application.CollectionPoints.Contracts;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DoacaoApp.IntegrationTests;

/// <summary>Serve um index.html com a mesma estrutura do build do frontend, no lugar do container do nginx.</summary>
public sealed class FakeIndexHtmlHandler : HttpMessageHandler
{
    public const string Template = """
        <!doctype html>
        <html lang="pt-BR">
          <head>
            <meta charset="UTF-8" />
            <meta name="description" content="Descrição padrão" />
            <meta name="robots" content="index, follow, max-image-preview:large" />
            <link rel="canonical" href="https://dooa.test/" />
            <meta property="og:url" content="https://dooa.test/" />
            <meta property="og:title" content="Título padrão" />
            <meta property="og:description" content="Descrição padrão" />
            <meta property="og:image" content="https://dooa.test/og-image.png" />
            <meta property="og:image:secure_url" content="https://dooa.test/og-image.png" />
            <meta property="og:image:type" content="image/png" />
            <meta property="og:image:width" content="1200" />
            <meta property="og:image:height" content="630" />
            <meta property="og:image:alt" content="Título padrão" />
            <meta name="twitter:title" content="Título padrão" />
            <meta name="twitter:description" content="Descrição padrão" />
            <meta name="twitter:image" content="https://dooa.test/og-image.png" />
            <meta name="twitter:image:alt" content="Título padrão" />
            <title>Título padrão</title>
            <script type="module" crossorigin src="/assets/index-abc.js"></script>
          </head>
          <body>
            <div id="root"></div>
          </body>
        </html>
        """;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Template) });
}

public sealed class SeoPagesFixture : IAsyncLifetime
{
    private readonly IntegrationTestFactory _database = new();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _database.InitializeAsync();
        Factory = _database.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Seo:SiteUrl", "https://dooa.test");
            builder.UseSetting("Seo:PublicDataCacheSeconds", "0");
            builder.ConfigureTestServices(services =>
            {
                services.PostConfigure<IpRateLimitOptions>(options => options.GeneralRules = []);
                services.AddHttpClient<IndexHtmlTemplate>().ConfigurePrimaryHttpMessageHandler(() => new FakeIndexHtmlHandler());
            });
        });
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await ((IAsyncLifetime)_database).DisposeAsync();
    }
}

public class SeoPagesTests(SeoPagesFixture fixture) : IClassFixture<SeoPagesFixture>
{
    private readonly WebApplicationFactory<Program> _factory = fixture.Factory;

    private HttpClient Anonymous() => _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Fact]
    public async Task PaginaDoPonto_TemTituloDescricaoCanonicalImagemEDadosEstruturados()
    {
        var point = await CreatePointAsync("Casa Solidária", needs: ["Leite em pó", "Fraldas"]);

        var response = await Anonymous().GetAsync($"/api/public/seo/pontos/{point.Id}/{point.Slug}");
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
        point.Slug.Should().Be("casa-solidaria-concordia-sc");
        html.Should().Contain("<title>Casa Solidária — ponto de doação em Concórdia/SC | Dooa</title>");
        html.Should().Contain($"<link rel=\"canonical\" href=\"https://dooa.test/pontos/{point.Id}/casa-solidaria-concordia-sc\" />");
        html.Should().Contain($"<meta property=\"og:image\" content=\"https://dooa.test/api/public/collection-points/{point.Id}/image\" />");
        html.Should().NotContain("og:image:width");
        html.Should().Contain("Precisa agora: Leite em pó, Fraldas.");
        html.Should().Contain("\"@type\":\"Place\"").And.Contain("\"@type\":\"PostalAddress\"").And.Contain("\"@type\":\"GeoCoordinates\"");
        html.Should().Contain("<noscript><main><h1>Casa Solidária</h1>");
        html.Should().Contain("<div id=\"root\"></div>").And.Contain("/assets/index-abc.js");
        html.Should().Contain("content=\"index, follow, max-image-preview:large\"");
    }

    [Fact]
    public async Task PaginaDoPonto_EscapaHtmlDoNome()
    {
        var point = await CreatePointAsync("Casa \"Aurora\" <b>Luz</b>");

        var html = await Anonymous().GetStringAsync($"/api/public/seo/pontos/{point.Id}/{point.Slug}");

        html.Should().NotContain("<b>Luz</b>");
        html.Should().Contain("Casa &quot;Aurora&quot; &lt;b&gt;Luz&lt;/b&gt;");
    }

    [Theory]
    [InlineData("")]
    [InlineData("/nome-antigo")]
    public async Task PaginaDoPonto_ComSlugErradoOuSemSlug_RedirecionaPermanenteParaACanonica(string suffix)
    {
        var point = await CreatePointAsync("Paróquia São José");

        var response = await Anonymous().GetAsync($"/api/public/seo/pontos/{point.Id}{suffix}");

        response.StatusCode.Should().Be(HttpStatusCode.MovedPermanently);
        response.Headers.Location!.ToString().Should().Be($"/pontos/{point.Id}/{point.Slug}");
    }

    [Theory]
    [InlineData("/api/public/seo/pontos/00000000-0000-0000-0000-000000000001/qualquer")]
    [InlineData("/api/public/seo/pontos/nao-e-um-id")]
    [InlineData("/api/public/seo/not-found")]
    public async Task PaginaInexistente_Responde404ForaDoIndice(string path)
    {
        var response = await Anonymous().GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        html.Should().Contain("<title>Página não encontrada | Dooa</title>");
        html.Should().Contain("content=\"noindex, follow\"");
    }

    [Fact]
    public async Task Inicio_ListaOsPontosComLinksParaAsPaginas()
    {
        var point = await CreatePointAsync("Associação Vida");

        var html = await Anonymous().GetStringAsync("/api/public/seo/home");

        html.Should().Contain("<link rel=\"canonical\" href=\"https://dooa.test/\" />");
        html.Should().Contain("<h1>Dooa — pontos de doação e coleta</h1>");
        html.Should().Contain("\"@type\":\"ItemList\"");
        html.Should().Contain("og:image:width");
        html.Should().Contain($"<a href=\"/pontos/{point.Id}/associacao-vida-concordia-sc\">Associação Vida</a> — Concórdia/SC");
    }

    [Fact]
    public async Task Sitemap_IncluiAPaginaDeCadaPonto()
    {
        var point = await CreatePointAsync("Igreja Matriz");

        var response = await Anonymous().GetAsync("/api/public/seo/sitemap.xml");
        var xml = await response.Content.ReadAsStringAsync();

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/xml");
        xml.Should().StartWith("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        xml.Should().Contain("<loc>https://dooa.test/</loc>");
        xml.Should().Contain($"<loc>https://dooa.test/pontos/{point.Id}/igreja-matriz-concordia-sc</loc>");
    }

    [Fact]
    public async Task ImagemDoPonto_RedirecionaParaOStorage()
    {
        var point = await CreatePointAsync("Casa da Sopa");

        var response = await Anonymous().GetAsync($"/api/public/collection-points/{point.Id}/image");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.Host.Should().Be("fake-storage.test");
        response.Headers.CacheControl!.MaxAge.Should().Be(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public async Task PontoPublico_PorId_DevolveOsDadosComSlug()
    {
        var point = await CreatePointAsync("Banco de Alimentos");

        var fetched = await Anonymous().GetFromJsonAsync<CollectionPointResponse>($"/api/public/collection-points/{point.Id}");

        fetched!.Slug.Should().Be("banco-de-alimentos-concordia-sc");
        fetched.ImageUrl.Should().NotBeNull();
    }

    private async Task<CollectionPointResponse> CreatePointAsync(string name, IReadOnlyList<string>? needs = null)
    {
        var admin = _factory.CreateClient();
        await TestApi.AuthenticateAsAdminAsync(admin);
        var created = await TestApi.CreatePointAsync(admin);

        var update = await admin.PutAsJsonAsync($"/api/collection-points/{created.Id}", new
        {
            name,
            type = "Church",
            street = "Rua das Flores",
            number = "100",
            complement = (string?)null,
            district = "Centro",
            city = "Concórdia",
            state = "SC",
            zipCode = "89700000",
            responsibleName = "Maria",
            email = "maria@doacao.org",
            phone = "49998765432",
            latitude = TestApi.ConcordiaLatitude,
            longitude = TestApi.ConcordiaLongitude,
            openingHours = "Seg a sex, 8h às 17h",
            needs = needs ?? [],
        });
        update.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await update.Content.ReadFromJsonAsync<CollectionPointResponse>())!;
    }
}
