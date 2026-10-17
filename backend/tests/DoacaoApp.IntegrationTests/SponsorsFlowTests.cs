using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Sponsors.Contracts;
using FluentAssertions;
using Xunit;

namespace DoacaoApp.IntegrationTests;

public class SponsorsFlowTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private const string ManagerPassword = "SenhaGestor1";

    [Fact]
    public async Task PaginaPublica_MostraSoOsAtivos_DoDiamanteAPrata()
    {
        var admin = await CreateAdminClientAsync();
        var silver = await CreateSponsorAsync(admin, $"Prata {Guid.NewGuid():N}", "Silver");
        var diamond = await CreateSponsorAsync(admin, $"Diamante {Guid.NewGuid():N}", "Diamond");
        var gold = await CreateSponsorAsync(admin, $"Ouro {Guid.NewGuid():N}", "Gold");
        var inactive = await CreateSponsorAsync(admin, $"Inativo {Guid.NewGuid():N}", "Diamond");
        (await admin.PutAsJsonAsync($"/api/sponsors/{inactive.Id}", new
        {
            name = inactive.Name,
            tier = "Diamond",
            websiteUrl = (string?)null,
            isActive = false,
            logoFileId = (Guid?)null,
        })).StatusCode.Should().Be(HttpStatusCode.OK);

        var publicList = await factory.CreateClient().GetFromJsonAsync<List<SponsorResponse>>("/api/public/sponsors");

        var ids = publicList!.Select(s => s.Id).ToList();
        ids.Should().NotContain(inactive.Id);
        ids.Where(id => id == diamond.Id || id == gold.Id || id == silver.Id)
            .Should().ContainInOrder(diamond.Id, gold.Id, silver.Id);
        publicList.Single(s => s.Id == diamond.Id).LogoUrl.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Admin_PodeRemoverPatrocinador()
    {
        var admin = await CreateAdminClientAsync();
        var sponsor = await CreateSponsorAsync(admin, $"Removido {Guid.NewGuid():N}", "Gold");

        (await admin.DeleteAsync($"/api/sponsors/{sponsor.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var all = await admin.GetFromJsonAsync<List<SponsorResponse>>("/api/sponsors");
        all!.Should().NotContain(s => s.Id == sponsor.Id);
    }

    [Fact]
    public async Task SiteInvalido_DeveSerRecusado()
    {
        var admin = await CreateAdminClientAsync();
        var logoId = await TestApi.UploadPointImageAsync(admin);

        var response = await admin.PostAsJsonAsync("/api/sponsors", new
        {
            name = "Igreja",
            tier = "Gold",
            websiteUrl = "javascript:alert(1)",
            logoFileId = logoId,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SoOAdmin_GerenciaPatrocinadores()
    {
        var admin = await CreateAdminClientAsync();
        var point = await TestApi.CreatePointAsync(admin);
        var manager = await CreateManagerClientAsync(admin, point.Id);
        var sponsor = await CreateSponsorAsync(admin, $"Protegido {Guid.NewGuid():N}", "Silver");

        (await manager.GetAsync("/api/sponsors")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await manager.PostAsJsonAsync("/api/sponsors", new { name = "Gestor", tier = "Diamond", logoFileId = Guid.NewGuid() }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await manager.DeleteAsync($"/api/sponsors/{sponsor.Id}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await factory.CreateClient().GetAsync("/api/sponsors")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<SponsorResponse> CreateSponsorAsync(HttpClient admin, string name, string tier)
    {
        var logoId = await TestApi.UploadPointImageAsync(admin);
        var response = await admin.PostAsJsonAsync("/api/sponsors", new
        {
            name,
            tier,
            websiteUrl = "https://exemplo.org.br",
            logoFileId = logoId,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<SponsorResponse>())!;
    }

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var admin = factory.CreateClient();
        await TestApi.AuthenticateAsAdminAsync(admin);
        return admin;
    }

    private async Task<HttpClient> CreateManagerClientAsync(HttpClient admin, Guid pointId)
    {
        var email = $"gestor-{Guid.NewGuid():N}@doacao.local";
        (await admin.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = ManagerPassword,
            fullName = "Gestor do Ponto",
            role = "PointManager",
            collectionPointId = pointId,
        })).StatusCode.Should().Be(HttpStatusCode.OK);

        var manager = factory.CreateClient();
        var login = await manager.PostAsJsonAsync("/api/auth/login", new { email, password = ManagerPassword });
        login.EnsureSuccessStatusCode();
        var auth = (await login.Content.ReadFromJsonAsync<AuthResult>())!;
        manager.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return manager;
    }
}
