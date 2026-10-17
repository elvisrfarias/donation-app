using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AspNetCoreRateLimit;
using DoacaoApp.Application.Common.Abstractions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DoacaoApp.IntegrationTests;

/// <summary>
/// Mesma aplicação do <see cref="IntegrationTestFactory"/>, mas sem as regras de rate limit: estes testes fazem
/// muitos logins seguidos do mesmo "IP" e esbarrariam no limite de 5 logins a cada 5 minutos.
/// </summary>
public sealed class AuthSecurityFlowFixture : IAsyncLifetime
{
    private readonly IntegrationTestFactory _database = new();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _database.InitializeAsync();
        Factory = _database.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.PostConfigure<IpRateLimitOptions>(options => options.GeneralRules = [])));
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await ((IAsyncLifetime)_database).DisposeAsync();
    }
}

public class AuthSecurityFlowTests(AuthSecurityFlowFixture fixture) : IClassFixture<AuthSecurityFlowFixture>
{
    private readonly WebApplicationFactory<Program> _factory = fixture.Factory;

    private const string InitialPassword = "SenhaInicial1";
    private const string AuthenticatedProbePath = "/api/notices";

    [Fact]
    public async Task Cadastro_DeveDevolverSoOUsuario_SemTokensDoCriado()
    {
        var admin = await CreateAdminClientAsync();
        var email = UniqueEmail();

        var response = await admin.PostAsJsonAsync("/api/auth/register", NewAdminBody(email));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        json.RootElement.GetProperty("email").GetString().Should().Be(email);
        json.RootElement.GetProperty("isActive").GetBoolean().Should().BeTrue();
        json.RootElement.TryGetProperty("accessToken", out _).Should().BeFalse();
        json.RootElement.TryGetProperty("refreshToken", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Desativacao_DeveDerrubarAccessTokenERefreshTokenJaEmitidos()
    {
        var admin = await CreateAdminClientAsync();
        var (userId, email) = await RegisterAdminAsync(admin);
        var session = await LoginAsync(email, InitialPassword);

        (await admin.PostAsync($"/api/auth/users/{userId}/deactivate", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await GetWithTokenAsync(session.AccessToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await RefreshAsync(session.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MudancaDePapel_DeveDerrubarAsSessoesDoUsuario()
    {
        var admin = await CreateAdminClientAsync();
        var (userId, email) = await RegisterAdminAsync(admin);
        var session = await LoginAsync(email, InitialPassword);
        var point = await TestApi.CreatePointAsync(admin);

        var update = await admin.PutAsJsonAsync($"/api/auth/users/{userId}", new
        {
            email,
            fullName = "Rebaixado",
            role = "Viewer",
            collectionPointId = point.Id,
        });

        update.StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetWithTokenAsync(session.AccessToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await RefreshAsync(session.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReusoDeRefreshTokenRevogado_DeveRevogarTodasAsSessoesDoUsuario()
    {
        var admin = await CreateAdminClientAsync();
        var (_, email) = await RegisterAdminAsync(admin);
        var stolenSession = await LoginAsync(email, InitialPassword);
        var otherSession = await LoginAsync(email, InitialPassword);

        (await _factory.CreateClient().PostAsJsonAsync("/api/auth/revoke", new { refreshToken = stolenSession.RefreshToken }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await RefreshAsync(stolenSession.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await RefreshAsync(otherSession.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RotacaoConcorrente_DeveRecusarSoOTokenAntigo_SemDerrubarONovo()
    {
        var admin = await CreateAdminClientAsync();
        var (_, email) = await RegisterAdminAsync(admin);
        var session = await LoginAsync(email, InitialPassword);

        var rotated = await RefreshAsync(session.RefreshToken);
        rotated.StatusCode.Should().Be(HttpStatusCode.OK);
        var rotatedSession = (await rotated.Content.ReadFromJsonAsync<AuthResult>())!;

        (await RefreshAsync(session.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await RefreshAsync(rotatedSession.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TrocaDeSenha_DeveManterSoASessaoCorrente()
    {
        var admin = await CreateAdminClientAsync();
        var (_, email) = await RegisterAdminAsync(admin);
        var currentSession = await LoginAsync(email, InitialPassword);
        var otherSession = await LoginAsync(email, InitialPassword);

        var change = await PostWithTokenAsync(currentSession.AccessToken, "/api/auth/change-password", new
        {
            currentPassword = InitialPassword,
            newPassword = "SenhaTrocada2",
        });

        change.StatusCode.Should().Be(HttpStatusCode.OK);
        var renewed = (await change.Content.ReadFromJsonAsync<AuthResult>())!;
        (await GetWithTokenAsync(renewed.AccessToken)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetWithTokenAsync(currentSession.AccessToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await RefreshAsync(otherSession.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await PostLoginAsync(email, InitialPassword)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await PostLoginAsync(email, "SenhaTrocada2")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TrocaDeSenha_ComSenhaAtualErrada_DeveRetornar400()
    {
        var admin = await CreateAdminClientAsync();
        var (_, email) = await RegisterAdminAsync(admin);
        var session = await LoginAsync(email, InitialPassword);

        var change = await PostWithTokenAsync(session.AccessToken, "/api/auth/change-password", new
        {
            currentPassword = "SenhaErrada9",
            newPassword = "SenhaTrocada2",
        });

        change.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await GetWithTokenAsync(session.AccessToken)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RedefinicaoDeSenhaPeloAdmin_DeveDerrubarAsSessoesEAceitarANovaSenha()
    {
        var admin = await CreateAdminClientAsync();
        var (userId, email) = await RegisterAdminAsync(admin);
        var session = await LoginAsync(email, InitialPassword);

        (await admin.PostAsJsonAsync($"/api/auth/users/{userId}/reset-password", new { newPassword = "SenhaRedefinida3" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await GetWithTokenAsync(session.AccessToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await RefreshAsync(session.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await PostLoginAsync(email, InitialPassword)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await PostLoginAsync(email, "SenhaRedefinida3")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_DeContaDesativada_DeveRetornarOMesmoErroDeCredenciaisInvalidas()
    {
        var admin = await CreateAdminClientAsync();
        var (userId, email) = await RegisterAdminAsync(admin);
        await admin.PostAsync($"/api/auth/users/{userId}/deactivate", null);

        var lockedOut = await PostLoginAsync(email, InitialPassword);
        var unknown = await PostLoginAsync(UniqueEmail(), InitialPassword);

        lockedOut.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        unknown.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await ErrorCodeOf(lockedOut)).Should().Be("Auth.InvalidCredentials");
        (await ErrorCodeOf(unknown)).Should().Be("Auth.InvalidCredentials");
    }

    [Fact]
    public async Task EdicaoDeEmailETelefone_DeveValerNoLoginEDerrubarAsSessoes()
    {
        var admin = await CreateAdminClientAsync();
        var (userId, oldEmail) = await RegisterAdminAsync(admin);
        var session = await LoginAsync(oldEmail, InitialPassword);
        var newEmail = UniqueEmail();

        var update = await admin.PutAsJsonAsync($"/api/auth/users/{userId}", new
        {
            email = newEmail,
            fullName = "Nome Novo",
            phoneNumber = "49998765432",
            role = "Admin",
            collectionPointId = (Guid?)null,
        });

        update.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = (await update.Content.ReadFromJsonAsync<UserResponse>())!;
        updated.Email.Should().Be(newEmail);
        updated.FullName.Should().Be("Nome Novo");
        updated.PhoneNumber.Should().Be("(49) 99876-5432");
        (await RefreshAsync(session.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await PostLoginAsync(oldEmail, InitialPassword)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await PostLoginAsync(newEmail, InitialPassword)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task EdicaoParaEmailDeOutroUsuario_DeveSerRecusadaSemAlterarNada()
    {
        var admin = await CreateAdminClientAsync();
        var (userId, email) = await RegisterAdminAsync(admin);
        var (_, takenEmail) = await RegisterAdminAsync(admin);

        var update = await admin.PutAsJsonAsync($"/api/auth/users/{userId}", new
        {
            email = takenEmail.ToUpperInvariant(),
            fullName = "Não Deveria Mudar",
            phoneNumber = (string?)null,
            role = "Admin",
            collectionPointId = (Guid?)null,
        });

        update.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var users = (await admin.GetFromJsonAsync<List<UserResponse>>("/api/auth/users"))!;
        users.Single(u => u.Id == userId).Should().Match<UserResponse>(u => u.Email == email && u.FullName == "Usuário de Teste");
    }

    [Fact]
    public async Task EdicaoSoDeNomeETelefone_DeveManterAsSessoes()
    {
        var admin = await CreateAdminClientAsync();
        var (userId, email) = await RegisterAdminAsync(admin);
        var session = await LoginAsync(email, InitialPassword);

        var update = await admin.PutAsJsonAsync($"/api/auth/users/{userId}", new
        {
            email,
            fullName = "Só o Nome",
            phoneNumber = "(49) 3442-1234",
            role = "Admin",
            collectionPointId = (Guid?)null,
        });

        update.StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetWithTokenAsync(session.AccessToken)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var admin = _factory.CreateClient();
        await TestApi.AuthenticateAsAdminAsync(admin);
        return admin;
    }

    private static string UniqueEmail() => $"usuario-{Guid.NewGuid():N}@doacao.local";

    private static object NewAdminBody(string email) => new
    {
        email,
        password = InitialPassword,
        fullName = "Usuário de Teste",
        role = "Admin",
        collectionPointId = (Guid?)null,
    };

    private static async Task<(Guid Id, string Email)> RegisterAdminAsync(HttpClient admin)
    {
        var email = UniqueEmail();
        var response = await admin.PostAsJsonAsync("/api/auth/register", NewAdminBody(email));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = (await response.Content.ReadFromJsonAsync<UserResponse>())!;
        return (user.Id, email);
    }

    private Task<HttpResponseMessage> PostLoginAsync(string email, string password) =>
        _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { email, password });

    private async Task<AuthResult> LoginAsync(string email, string password)
    {
        var response = await PostLoginAsync(email, password);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<AuthResult>())!;
    }

    private Task<HttpResponseMessage> RefreshAsync(string refreshToken) =>
        _factory.CreateClient().PostAsJsonAsync("/api/auth/refresh", new { refreshToken });

    private HttpClient ClientWithToken(string accessToken)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    private Task<HttpResponseMessage> GetWithTokenAsync(string accessToken) =>
        ClientWithToken(accessToken).GetAsync(AuthenticatedProbePath);

    private Task<HttpResponseMessage> PostWithTokenAsync(string accessToken, string path, object body) =>
        ClientWithToken(accessToken).PostAsJsonAsync(path, body);

    private static async Task<string?> ErrorCodeOf(HttpResponseMessage response)
    {
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return json.RootElement.GetProperty("errorCode").GetString();
    }
}
