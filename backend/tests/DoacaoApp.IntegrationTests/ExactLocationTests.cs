using System.Net;
using System.Net.Http.Json;
using DoacaoApp.Application.CollectionPoints.Contracts;
using FluentAssertions;
using Xunit;

namespace DoacaoApp.IntegrationTests;

public class ExactLocationTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    [Fact]
    public async Task CriarPonto_SemCoordenadas_DeveRetornar400PedindoOLocalExato()
    {
        var client = factory.CreateClient();
        await TestApi.AuthenticateAsAdminAsync(client);

        var response = await TestApi.PostPointAsync(client, latitude: null, longitude: null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("local exato");
    }

    [Fact]
    public async Task CriarPonto_ComCoordenadas_DevolveAPosicaoGravada()
    {
        var client = factory.CreateClient();
        await TestApi.AuthenticateAsAdminAsync(client);

        var point = await TestApi.CreatePointAsync(client);

        point.Address.Latitude.Should().Be(TestApi.ConcordiaLatitude);
        point.Address.Longitude.Should().Be(TestApi.ConcordiaLongitude);
    }

    [Fact]
    public async Task EditarPonto_SemCoordenadas_DeveRetornar400EManterAPosicao()
    {
        var client = factory.CreateClient();
        await TestApi.AuthenticateAsAdminAsync(client);
        var point = await TestApi.CreatePointAsync(client);

        var response = await client.PutAsJsonAsync($"/api/collection-points/{point.Id}", new
        {
            name = point.Name,
            type = "Church",
            street = "Rua Nova",
            number = "1",
            complement = (string?)null,
            district = "Centro",
            city = "Concórdia",
            state = "SC",
            zipCode = "89700000",
            responsibleName = "Maria",
            email = "maria@doacao.org",
            phone = "11987654321",
            latitude = (double?)null,
            longitude = (double?)null,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var unchanged = await client.GetFromJsonAsync<CollectionPointResponse>($"/api/collection-points/{point.Id}");
        unchanged!.Address.Street.Should().Be("Rua das Flores");
        unchanged.Address.Latitude.Should().Be(TestApi.ConcordiaLatitude);
    }

    [Fact]
    public async Task PontosDoSeed_TodosTemPosicao()
    {
        var client = factory.CreateClient();

        var points = await client.GetFromJsonAsync<List<CollectionPointResponse>>("/api/public/collection-points");

        points.Should().NotBeEmpty();
        points!.Should().OnlyContain(p => p.Address.Latitude != null && p.Address.Longitude != null);
    }
}
