using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DoacaoApp.Application.CollectionPoints.Contracts;
using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Families.Contracts;
using DoacaoApp.Application.Storage.Contracts;
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

        var imageId = await UploadPointImageAsync(client);

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
            imageFileId = imageId,
        });
        pointResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var point = (await pointResponse.Content.ReadFromJsonAsync<CollectionPointResponse>())!;

        var familyResponse = await client.PostAsJsonAsync("/api/families", new
        {
            name = "Família Silva",
            cycleType = "Monthly",
            maxWithdrawalsPerCycle = 1,
        });
        familyResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var family = (await familyResponse.Content.ReadFromJsonAsync<FamilyResponse>())!;

        var beneficiaryResponse = await client.PostAsJsonAsync($"/api/families/{family.Id}/beneficiaries", new
        {
            name = "João Silva",
            cpf = "111.444.777-35",
            birthDate = (string?)null,
            registeredByPointId = point.Id,
        });
        beneficiaryResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var beneficiary = (await beneficiaryResponse.Content.ReadFromJsonAsync<BeneficiaryResponse>())!;

        var firstWithdrawal = await client.PostAsJsonAsync("/api/withdrawals", new
        {
            pointId = point.Id,
            familyId = family.Id,
            beneficiaryId = beneficiary.Id,
            occurredAtUtc = (string?)null,
            notes = "Cesta básica",
        });
        firstWithdrawal.StatusCode.Should().Be(HttpStatusCode.Created);

        var secondWithdrawal = await client.PostAsJsonAsync("/api/withdrawals", new
        {
            pointId = point.Id,
            familyId = family.Id,
            beneficiaryId = beneficiary.Id,
            occurredAtUtc = (string?)null,
            notes = "Tentativa repetida",
        });
        secondWithdrawal.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var publicList = await client.GetFromJsonAsync<List<CollectionPointResponse>>("/api/public/collection-points");
        publicList.Should().ContainSingle(p => p.Id == point.Id)
            .Which.ImageUrl.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CriarPonto_SemImagem_DeveRetornar400()
    {
        var client = factory.CreateClient();
        await AuthenticateAsAdminAsync(client);

        var response = await client.PostAsJsonAsync("/api/collection-points", new
        {
            name = "Ponto sem imagem",
            type = "Association",
            street = "Rua A",
            number = "1",
            complement = (string?)null,
            district = "Centro",
            city = "Chapecó",
            state = "SC",
            zipCode = "89800000",
            responsibleName = "José",
            email = "jose@doacao.org",
            phone = "49987654321",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>Faz upload de um PNG mínimo e devolve o id do arquivo armazenado.</summary>
    private static async Task<Guid> UploadPointImageAsync(HttpClient client)
    {
        using var content = new MultipartFormDataContent();
        var image = new ByteArrayContent(MinimalPng);
        image.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(image, "file", "ponto.png");
        content.Add(new StringContent("Public"), "visibility");

        var response = await client.PostAsync("/api/files", content);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var file = (await response.Content.ReadFromJsonAsync<StoredFileResponse>())!;
        return file.Id;
    }

    private static readonly byte[] MinimalPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");

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