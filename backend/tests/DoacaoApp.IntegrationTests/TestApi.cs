using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DoacaoApp.Application.CollectionPoints.Contracts;
using DoacaoApp.Application.Common.Abstractions;
using FluentAssertions;

namespace DoacaoApp.IntegrationTests;

/// <summary>Atalhos de chamadas à API compartilhados pelos testes de integração.</summary>
public static class TestApi
{
    public const double ConcordiaLatitude = -27.2342;
    public const double ConcordiaLongitude = -52.0278;

    private static readonly byte[] MinimalPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");

    public static async Task AuthenticateAsAdminAsync(HttpClient client)
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

    public static async Task<Guid> UploadPointImageAsync(HttpClient client)
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

    public static async Task<HttpResponseMessage> PostPointAsync(
        HttpClient client, double? latitude = ConcordiaLatitude, double? longitude = ConcordiaLongitude) =>
        await client.PostAsJsonAsync("/api/collection-points", new
        {
            name = $"Ponto {Guid.NewGuid():N}",
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
            phone = "11987654321",
            imageFileId = await UploadPointImageAsync(client),
            latitude,
            longitude,
        });

    public static async Task<CollectionPointResponse> CreatePointAsync(HttpClient client)
    {
        var response = await PostPointAsync(client);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<CollectionPointResponse>())!;
    }
}
