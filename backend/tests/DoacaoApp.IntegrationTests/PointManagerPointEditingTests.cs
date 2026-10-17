using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DoacaoApp.Application.CollectionPoints.Contracts;
using DoacaoApp.Application.Common.Abstractions;
using FluentAssertions;
using Xunit;

namespace DoacaoApp.IntegrationTests;

public class PointManagerPointEditingTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private const string ManagerPassword = "SenhaGestor1";

    [Fact]
    public async Task Gestor_PodeEditarTodosOsDadosDoProprioPonto()
    {
        var admin = await CreateAdminClientAsync();
        var point = await TestApi.CreatePointAsync(admin);
        var manager = await CreateManagerClientAsync(admin, point.Id);

        var response = await manager.PutAsJsonAsync($"/api/collection-points/{point.Id}", UpdatedPointBody("Ponto Renomeado pelo Gestor"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = (await response.Content.ReadFromJsonAsync<CollectionPointResponse>())!;
        updated.Name.Should().Be("Ponto Renomeado pelo Gestor");
        updated.Address.Street.Should().Be("Rua Nova");
        updated.Contact.ResponsibleName.Should().Be("João");
        updated.OpeningHours.Should().Be("Seg a sex, 8h às 17h");
    }

    [Fact]
    public async Task Gestor_NaoPodeEditarPontoDeOutroGestor()
    {
        var admin = await CreateAdminClientAsync();
        var ownPoint = await TestApi.CreatePointAsync(admin);
        var otherPoint = await TestApi.CreatePointAsync(admin);
        var manager = await CreateManagerClientAsync(admin, ownPoint.Id);

        var response = await manager.PutAsJsonAsync($"/api/collection-points/{otherPoint.Id}", UpdatedPointBody("Invasão"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var unchanged = await admin.GetFromJsonAsync<CollectionPointResponse>($"/api/collection-points/{otherPoint.Id}");
        unchanged!.Name.Should().Be(otherPoint.Name);
    }

    private static object UpdatedPointBody(string name) => new
    {
        name,
        type = "Church",
        street = "Rua Nova",
        number = "200",
        complement = (string?)null,
        district = "Centro",
        city = "Concórdia",
        state = "SC",
        zipCode = "89700000",
        responsibleName = "João",
        email = "joao@doacao.org",
        phone = "49998765432",
        latitude = TestApi.ConcordiaLatitude,
        longitude = TestApi.ConcordiaLongitude,
        openingHours = "Seg a sex, 8h às 17h",
    };

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var admin = factory.CreateClient();
        await TestApi.AuthenticateAsAdminAsync(admin);
        return admin;
    }

    private async Task<HttpClient> CreateManagerClientAsync(HttpClient admin, Guid pointId)
    {
        var email = $"gestor-{Guid.NewGuid():N}@doacao.local";
        var register = await admin.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = ManagerPassword,
            fullName = "Gestor do Ponto",
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
