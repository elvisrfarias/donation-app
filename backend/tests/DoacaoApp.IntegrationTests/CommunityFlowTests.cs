using System.Net;
using System.Net.Http.Json;
using DoacaoApp.Application.Activity.Contracts;
using DoacaoApp.Application.CollectionPoints.Contracts;
using DoacaoApp.Application.Donations.Contracts;
using DoacaoApp.Application.Notices.Contracts;
using FluentAssertions;
using Xunit;

namespace DoacaoApp.IntegrationTests;

public class CommunityFlowTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    [Fact]
    public async Task DoacaoPrometida_DeveAparecerParaOGestorEPoderSerRecebida()
    {
        var admin = factory.CreateClient();
        await TestApi.AuthenticateAsAdminAsync(admin);
        var point = await TestApi.CreatePointAsync(admin);

        var anonymous = factory.CreateClient();
        var pledge = await anonymous.PostAsJsonAsync($"/api/public/collection-points/{point.Id}/donations", new
        {
            donorName = "Carlos",
            donorPhone = "11987654321",
            description = "10kg de arroz",
        });
        pledge.StatusCode.Should().Be(HttpStatusCode.Created);
        var donation = (await pledge.Content.ReadFromJsonAsync<DonationResponse>())!;

        var listed = await admin.GetFromJsonAsync<List<DonationResponse>>(
            $"/api/collection-points/{point.Id}/donations");
        listed.Should().ContainSingle(d => d.Id == donation.Id && d.Status == "Pledged");

        var received = await admin.PostAsJsonAsync(
            $"/api/collection-points/{point.Id}/donations/{donation.Id}/receive", new { });
        received.StatusCode.Should().Be(HttpStatusCode.OK);

        var again = await admin.PostAsJsonAsync(
            $"/api/collection-points/{point.Id}/donations/{donation.Id}/cancel", new { });
        again.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DoacaoPrometida_SemNome_DeveRetornar400()
    {
        var admin = factory.CreateClient();
        await TestApi.AuthenticateAsAdminAsync(admin);
        var point = await TestApi.CreatePointAsync(admin);

        var response = await factory.CreateClient().PostAsJsonAsync(
            $"/api/public/collection-points/{point.Id}/donations",
            new { donorName = "", donorPhone = (string?)null, description = "Roupas" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListaDeDoacoes_SemAutenticacao_DeveRetornar401()
    {
        var response = await factory.CreateClient().GetAsync(
            $"/api/collection-points/{Guid.NewGuid()}/donations");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MuralDeAvisos_SoExpoeAoPublicoOsAvisosPublicos()
    {
        var admin = factory.CreateClient();
        await TestApi.AuthenticateAsAdminAsync(admin);
        var point = await TestApi.CreatePointAsync(admin);

        await PublishNoticeAsync(admin, point.Id, "Falta leite", "Public");
        await PublishNoticeAsync(admin, point.Id, "Reunião de gestores", "Managers");

        var publicNotices = await factory.CreateClient().GetFromJsonAsync<List<NoticeResponse>>(
            $"/api/public/collection-points/{point.Id}/notices");
        publicNotices.Should().ContainSingle().Which.Title.Should().Be("Falta leite");

        var network = await admin.GetFromJsonAsync<List<NoticeResponse>>("/api/notices");
        network.Should().Contain(n => n.Title == "Reunião de gestores" && n.PointName == point.Name);
    }

    [Fact]
    public async Task AvisoArquivado_SomeDoMural()
    {
        var admin = factory.CreateClient();
        await TestApi.AuthenticateAsAdminAsync(admin);
        var point = await TestApi.CreatePointAsync(admin);
        var notice = await PublishNoticeAsync(admin, point.Id, "Mutirão", "Public");

        var archive = await admin.DeleteAsync($"/api/collection-points/{point.Id}/notices/{notice.Id}");
        archive.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var publicNotices = await factory.CreateClient().GetFromJsonAsync<List<NoticeResponse>>(
            $"/api/public/collection-points/{point.Id}/notices");
        publicNotices.Should().BeEmpty();
    }

    [Fact]
    public async Task FeedDeAtividade_ReuneDoacaoEAviso()
    {
        var admin = factory.CreateClient();
        await TestApi.AuthenticateAsAdminAsync(admin);
        var point = await TestApi.CreatePointAsync(admin);
        await PublishNoticeAsync(admin, point.Id, "Aviso do feed", "Public");
        await factory.CreateClient().PostAsJsonAsync($"/api/public/collection-points/{point.Id}/donations", new
        {
            donorName = "Carlos",
            donorPhone = (string?)null,
            description = "Roupas",
        });

        var feed = await admin.GetFromJsonAsync<ActivityFeedResponse>($"/api/collection-points/{point.Id}/activity");

        feed!.Items.Select(i => i.Kind).Should().Contain([ActivityKinds.Notice, ActivityKinds.Donation]);
    }

    private static async Task<NoticeResponse> PublishNoticeAsync(
        HttpClient client, Guid pointId, string title, string audience)
    {
        var response = await client.PostAsJsonAsync($"/api/collection-points/{pointId}/notices", new
        {
            title,
            message = "Mensagem do aviso",
            audience,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<NoticeResponse>())!;
    }
}
