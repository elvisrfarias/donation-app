using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DoacaoApp.Application.Beneficiaries.Contracts;
using DoacaoApp.Application.CollectionPoints.Contracts;
using DoacaoApp.Application.Common.Abstractions;
using FluentAssertions;
using Xunit;

namespace DoacaoApp.IntegrationTests;

/// <summary>
/// Fluxo do balcão com um gestor de outro ponto: a busca por CPF devolve só a projeção mascarada,
/// nada do cadastro é lido/alterado sem vínculo, e depois de vincular a retirada é registrada.
/// </summary>
public class BeneficiaryPrivacyFlowTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private const string MariaCpf = "529.982.247-25";
    private const string ManagerPassword = "Gestor123456";

    [Fact]
    public async Task GestorDeOutroPonto_SoVeDadosMascaradosAteVincularEEntaoRegistraARetirada()
    {
        var admin = factory.CreateClient();
        await TestApi.AuthenticateAsAdminAsync(admin);
        var registeringPoint = await TestApi.CreatePointAsync(admin);
        var counterPoint = await TestApi.CreatePointAsync(admin);
        var stockItemId = await AddClothingStockAsync(admin, counterPoint.Id);
        var maria = await RegisterBeneficiaryAsync(admin, registeringPoint.Id);
        var manager = await CreateManagerClientAsync(admin, counterPoint.Id);

        var lookup = await LookupAsync(manager, MariaCpf);
        lookup.Id.Should().Be(maria.Id);
        lookup.Name.Should().Be("Maria S.");
        lookup.Cpf.Should().Be("***.982.247-**");
        lookup.IsActive.Should().BeTrue();
        lookup.IsLinkedToMyPoint.Should().BeFalse();
        lookup.Beneficiary.Should().BeNull();

        (await manager.GetAsync($"/api/beneficiaries/{maria.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await manager.GetAsync($"/api/beneficiaries/{maria.Id}/withdrawals?pointId={counterPoint.Id}"))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        var kinship = await manager.PostAsJsonAsync($"/api/beneficiaries/{maria.Id}/kinships",
            new { relatedBeneficiaryCpf = "111.444.777-35", type = "Filho" });
        kinship.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await kinship.Content.ReadAsStringAsync()).Should().Contain("\"Beneficiary.NotFound\"");

        var beforeLink = await manager.PostAsJsonAsync("/api/withdrawals", ClothingWithdrawal(counterPoint.Id, maria.Id, stockItemId));
        beforeLink.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var link = await manager.PostAsJsonAsync($"/api/beneficiaries/{maria.Id}/points", new { pointId = counterPoint.Id });
        link.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterLink = await LookupAsync(manager, MariaCpf);
        afterLink.IsLinkedToMyPoint.Should().BeTrue();
        afterLink.Beneficiary!.Cpf.Should().Be(MariaCpf);

        var backdated = await manager.PostAsJsonAsync("/api/withdrawals",
            ClothingWithdrawal(counterPoint.Id, maria.Id, stockItemId, DateTime.UtcNow.AddDays(-10)));
        backdated.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var withdrawal = await manager.PostAsJsonAsync("/api/withdrawals", ClothingWithdrawal(counterPoint.Id, maria.Id, stockItemId));
        withdrawal.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task BuscaPorCpf_NaoAceitaMaisOCpfNaUrl()
    {
        var admin = factory.CreateClient();
        await TestApi.AuthenticateAsAdminAsync(admin);

        var response = await admin.GetAsync("/api/beneficiaries/by-cpf/52998224725");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);
    }

    private static async Task<BeneficiaryCpfLookupResponse> LookupAsync(HttpClient client, string cpf)
    {
        var response = await client.PostAsJsonAsync("/api/beneficiaries/lookup-by-cpf", new { cpf });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<BeneficiaryCpfLookupResponse>())!;
    }

    private static object ClothingWithdrawal(Guid pointId, Guid beneficiaryId, Guid stockItemId, DateTime? occurredAtUtc = null) =>
        new
        {
            pointId,
            beneficiaryId,
            items = new[] { new { stockItemId, quantity = 1 } },
            occurredAtUtc,
            notes = (string?)null,
        };

    private static async Task<BeneficiaryResponse> RegisterBeneficiaryAsync(HttpClient admin, Guid pointId)
    {
        var response = await admin.PostAsJsonAsync("/api/beneficiaries", new
        {
            name = "Maria Aparecida Souza",
            cpf = MariaCpf,
            email = "maria@example.com",
            phones = new[] { "11987654321" },
            address = (object?)null,
            birthDate = "1985-03-10",
            registeredByPointId = pointId,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<BeneficiaryResponse>())!;
    }

    private static async Task<Guid> AddClothingStockAsync(HttpClient admin, Guid pointId)
    {
        var withCategory = await admin.PostAsJsonAsync($"/api/collection-points/{pointId}/categories", new { name = "Roupas", icon = "shirt" });
        withCategory.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        var categoryId = (await withCategory.Content.ReadFromJsonAsync<CollectionPointResponse>())!.Categories[0].Id;

        var withItem = await admin.PostAsJsonAsync($"/api/collection-points/{pointId}/stock-items", new
        {
            name = "Camiseta",
            unit = "un",
            categoryId,
            initialQuantity = 10,
            goal = 0,
            alertThreshold = 1,
            isTracked = true,
        });
        withItem.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        return (await withItem.Content.ReadFromJsonAsync<CollectionPointResponse>())!.StockItems![0].Id;
    }

    private async Task<HttpClient> CreateManagerClientAsync(HttpClient admin, Guid pointId)
    {
        var email = $"gestor-{Guid.NewGuid():N}@doacao.local";
        var register = await admin.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = ManagerPassword,
            fullName = "Gestor do Balcão",
            role = "PointManager",
            collectionPointId = pointId,
        });
        register.StatusCode.Should().Be(HttpStatusCode.OK);

        var manager = factory.CreateClient();
        var login = await manager.PostAsJsonAsync("/api/auth/login", new { email, password = ManagerPassword });
        login.EnsureSuccessStatusCode();
        var auth = (await login.Content.ReadFromJsonAsync<AuthResult>())!;
        manager.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return manager;
    }
}
