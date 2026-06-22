using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DoacaoApp.Application.CollectionPoints.Contracts;
using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Families.Contracts;
using FluentAssertions;
using Xunit;

namespace DoacaoApp.IntegrationTests;

public class WithdrawalFlowTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    [Fact]
    public async Task ListaPublicaDePontos_DeveSerAcessivelSemAutenticacao()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/public/collection-points");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task FluxoCompleto_DeveRegistrarRetiradaEBloquearSegundaNoMesmoCiclo()
    {
        var client = factory.CreateClient();
        await AuthenticateAsAdminAsync(client);

        // 1) Cria o ponto de coleta.
        var pointResponse = await client.PostAsJsonAsync("/api/collection-points", new
        {
            name = "Paróquia São José",
            type = "Church",
            street = "Rua das Flores",
            number = "100",
            complement = (string?)null,
            district = "Centro",
            city = "São Paulo",
            state = "SP",
            zipCode = "01001000",
            responsibleName = "Maria",
            email = "maria@doacao.org",
            phone = "11987654321",
        });
        pointResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var point = (await pointResponse.Content.ReadFromJsonAsync<CollectionPointResponse>())!;

        // 2) Cria a família com limite de 1 retirada por mês.
        var familyResponse = await client.PostAsJsonAsync("/api/families", new
        {
            name = "Família Silva",
            cycleType = "Monthly",
            maxWithdrawalsPerCycle = 1,
        });
        familyResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var family = (await familyResponse.Content.ReadFromJsonAsync<FamilyResponse>())!;

        // 3) Cadastra e vincula o beneficiário.
        var beneficiaryResponse = await client.PostAsJsonAsync($"/api/families/{family.Id}/beneficiaries", new
        {
            name = "João Silva",
            cpf = "111.444.777-35",
            birthDate = (string?)null,
            registeredByPointId = point.Id,
        });
        beneficiaryResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var beneficiary = (await beneficiaryResponse.Content.ReadFromJsonAsync<BeneficiaryResponse>())!;

        // 4) Primeira retirada -> 201 Created.
        var firstWithdrawal = await client.PostAsJsonAsync("/api/withdrawals", new
        {
            pointId = point.Id,
            familyId = family.Id,
            beneficiaryId = beneficiary.Id,
            occurredAtUtc = (string?)null,
            notes = "Cesta básica",
        });
        firstWithdrawal.StatusCode.Should().Be(HttpStatusCode.Created);

        // 5) Segunda retirada no mesmo ciclo -> 409 Conflict (regra de equidade municipal).
        var secondWithdrawal = await client.PostAsJsonAsync("/api/withdrawals", new
        {
            pointId = point.Id,
            familyId = family.Id,
            beneficiaryId = beneficiary.Id,
            occurredAtUtc = (string?)null,
            notes = "Tentativa repetida",
        });
        secondWithdrawal.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // 6) A lista pública agora exibe o ponto criado.
        var publicList = await client.GetFromJsonAsync<List<CollectionPointResponse>>("/api/public/collection-points");
        publicList.Should().ContainSingle(p => p.Id == point.Id);
    }

    private static async Task AuthenticateAsAdminAsync(HttpClient client)
    {
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@doacao.local",
            password = "Admin@123456",
        });

        loginResponse.EnsureSuccessStatusCode();
        var auth = (await loginResponse.Content.ReadFromJsonAsync<AuthResult>())!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
    }
}