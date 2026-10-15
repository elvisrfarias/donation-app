using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DoacaoApp.Application.CollectionPoints.Contracts;
using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Beneficiaries.Contracts;
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
            latitude = -23.5505,
            longitude = -46.6333,
        });
        pointResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var point = (await pointResponse.Content.ReadFromJsonAsync<CollectionPointResponse>())!;

        var basketTemplateId = await SetUpBasketAsync(client, point.Id);

        var beneficiaryResponse = await client.PostAsJsonAsync("/api/beneficiaries", new
        {
            name = "João Silva",
            cpf = "111.444.777-35",
            email = "joao@example.com",
            phones = new[] { "11987654321" },
            address = (object?)null,
            birthDate = "1990-05-20",
            registeredByPointId = point.Id,
        });
        beneficiaryResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var beneficiary = (await beneficiaryResponse.Content.ReadFromJsonAsync<BeneficiaryResponse>())!;

        var firstWithdrawal = await client.PostAsJsonAsync("/api/withdrawals",
            BasketWithdrawal(point.Id, beneficiary.Id, basketTemplateId, "Cesta básica"));
        firstWithdrawal.StatusCode.Should().Be(HttpStatusCode.Created);

        var secondWithdrawal = await client.PostAsJsonAsync("/api/withdrawals",
            BasketWithdrawal(point.Id, beneficiary.Id, basketTemplateId, "Tentativa repetida"));
        secondWithdrawal.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var publicList = await client.GetFromJsonAsync<List<CollectionPointResponse>>("/api/public/collection-points");
        publicList.Should().ContainSingle(p => p.Id == point.Id)
            .Which.ImageUrl.Should().NotBeNullOrEmpty();
    }

    private static object BasketWithdrawal(Guid pointId, Guid beneficiaryId, (Guid StockItemId, Guid TemplateId) basket, string notes) =>
        new
        {
            pointId,
            beneficiaryId,
            items = new[] { new { stockItemId = basket.StockItemId, quantity = 1 } },
            occurredAtUtc = (string?)null,
            notes,
            basketTemplateId = basket.TemplateId,
            basketQuantity = 1,
        };

    private static async Task<(Guid StockItemId, Guid TemplateId)> SetUpBasketAsync(HttpClient client, Guid pointId)
    {
        (await client.PutAsJsonAsync($"/api/collection-points/{pointId}/withdrawal-rule", new
        {
            cycleType = 3,
            maxWithdrawalsPerCycle = 1,
        })).StatusCode.Should().Be(HttpStatusCode.OK);

        var category = await PostPointAsync(client, pointId, "categories", new { name = "Alimentos", icon = "food" });
        var categoryId = category.GetProperty("categories")[0].GetProperty("id").GetGuid();

        var withItem = await PostPointAsync(client, pointId, "stock-items", new
        {
            name = "Arroz 1kg",
            unit = "un",
            categoryId,
            initialQuantity = 100,
            goal = 0,
            alertThreshold = 5,
            isTracked = true,
        });
        var stockItemId = withItem.GetProperty("stockItems")[0].GetProperty("id").GetGuid();

        var withTemplate = await PostPointAsync(client, pointId, "basket-templates", new
        {
            name = "Cesta básica",
            items = new[] { new { stockItemId, quantity = 1 } },
        });
        var templateId = withTemplate.GetProperty("basketTemplates")[0].GetProperty("id").GetGuid();

        return (stockItemId, templateId);
    }

    private static async Task<System.Text.Json.JsonElement> PostPointAsync(
        HttpClient client, Guid pointId, string resource, object body)
    {
        var response = await client.PostAsJsonAsync($"/api/collection-points/{pointId}/{resource}", body);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        using var json = await System.Text.Json.JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return json.RootElement.Clone();
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

        using var json = await System.Text.Json.JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return json.RootElement.GetProperty("id").GetGuid();
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