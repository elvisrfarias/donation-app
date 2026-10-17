using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using AspNetCoreRateLimit;
using DoacaoApp.Application.Common.Abstractions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace DoacaoApp.IntegrationTests;

/// <summary>Guarda os pedidos de redefinição em vez de enviar e-mail, para o teste usar o token do "link".</summary>
public sealed class CapturingPasswordResetNotifier : IPasswordResetNotifier
{
    private readonly ConcurrentQueue<PasswordResetTicket> _tickets = new();

    public Task NotifyAsync(PasswordResetTicket ticket, CancellationToken cancellationToken = default)
    {
        _tickets.Enqueue(ticket);
        return Task.CompletedTask;
    }

    public IReadOnlyList<PasswordResetTicket> TicketsFor(string email) =>
        _tickets.Where(t => string.Equals(t.Email, email, StringComparison.OrdinalIgnoreCase)).ToList();
}

/// <summary>Aplicação sem rate limit e sem intervalo mínimo entre pedidos, com os e-mails capturados em memória.</summary>
public sealed class PasswordRecoveryFlowFixture : IAsyncLifetime
{
    private readonly IntegrationTestFactory _database = new();

    public CapturingPasswordResetNotifier Notifier { get; } = new();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _database.InitializeAsync();
        Factory = _database.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Security:PasswordResetCooldownSeconds", "0");
            builder.ConfigureTestServices(services =>
            {
                services.PostConfigure<IpRateLimitOptions>(options => options.GeneralRules = []);
                services.RemoveAll<IPasswordResetNotifier>();
                services.AddSingleton<IPasswordResetNotifier>(Notifier);
            });
        });
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await ((IAsyncLifetime)_database).DisposeAsync();
    }
}

public class PasswordRecoveryFlowTests(PasswordRecoveryFlowFixture fixture) : IClassFixture<PasswordRecoveryFlowFixture>
{
    private const string InitialPassword = "SenhaInicial1";
    private const string NewPassword = "SenhaRecuperada2";

    private readonly WebApplicationFactory<Program> _factory = fixture.Factory;
    private readonly CapturingPasswordResetNotifier _notifier = fixture.Notifier;

    [Fact]
    public async Task EsqueciMinhaSenha_DeveTrocarASenhaPeloLinkEDerrubarAsSessoes()
    {
        var email = await RegisterUserAsync();
        var session = await LoginAsync(email, InitialPassword);

        (await RequestResetAsync(email)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var token = _notifier.TicketsFor(email).Should().ContainSingle().Subject.Token;

        (await CompleteResetAsync(token, NewPassword)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await RefreshAsync(session.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await PostLoginAsync(email, InitialPassword)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await PostLoginAsync(email, NewPassword)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LinkDeRedefinicao_SoPodeSerUsadoUmaVez()
    {
        var email = await RegisterUserAsync();
        await RequestResetAsync(email);
        var token = _notifier.TicketsFor(email).Single().Token;

        await CompleteResetAsync(token, NewPassword);
        var reuse = await CompleteResetAsync(token, "OutraSenha3");

        reuse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await PostLoginAsync(email, NewPassword)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task NovoPedido_DeveInvalidarOLinkAnterior()
    {
        var email = await RegisterUserAsync();
        await RequestResetAsync(email);
        await RequestResetAsync(email);
        var tickets = _notifier.TicketsFor(email);

        (await CompleteResetAsync(tickets[0].Token, NewPassword)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CompleteResetAsync(tickets[1].Token, NewPassword)).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task SenhaFracaNoLink_DeveSerRecusadaSemGastarOToken()
    {
        var email = await RegisterUserAsync();
        await RequestResetAsync(email);
        var token = _notifier.TicketsFor(email).Single().Token;

        (await CompleteResetAsync(token, "fraca")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CompleteResetAsync(token, NewPassword)).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task TokenInventado_DeveSerRecusado()
    {
        (await CompleteResetAsync("token-que-nao-existe", NewPassword)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task EmailSemConta_DeveResponderIgualSemEnviarNada()
    {
        var email = UniqueEmail();

        (await RequestResetAsync(email)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        _notifier.TicketsFor(email).Should().BeEmpty();
    }

    [Fact]
    public async Task ContaDesativada_NaoRecebeLinkDeRedefinicao()
    {
        var admin = await CreateAdminClientAsync();
        var email = UniqueEmail();
        var user = await RegisterUserAsync(admin, email);
        await admin.PostAsync($"/api/auth/users/{user}/deactivate", null);

        (await RequestResetAsync(email)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        _notifier.TicketsFor(email).Should().BeEmpty();
    }

    [Fact]
    public async Task ContaBloqueadaPorTentativasErradas_VoltaAEntrarDepoisDeRedefinir()
    {
        var email = await RegisterUserAsync();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await PostLoginAsync(email, "SenhaErrada9");
        }

        await RequestResetAsync(email);
        await CompleteResetAsync(_notifier.TicketsFor(email).Single().Token, NewPassword);

        (await PostLoginAsync(email, NewPassword)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PedidosSeguidos_DentroDoIntervaloMinimo_EnviamUmSoEmail()
    {
        await using var factoryWithCooldown = _factory.WithWebHostBuilder(builder =>
            builder.UseSetting("Security:PasswordResetCooldownSeconds", "60"));
        var email = await RegisterUserAsync();
        var client = factoryWithCooldown.CreateClient();

        await client.PostAsJsonAsync("/api/auth/forgot-password", new { email });
        (await client.PostAsJsonAsync("/api/auth/forgot-password", new { email }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        _notifier.TicketsFor(email).Should().ContainSingle();
    }

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var admin = _factory.CreateClient();
        await TestApi.AuthenticateAsAdminAsync(admin);
        return admin;
    }

    private static string UniqueEmail() => $"usuario-{Guid.NewGuid():N}@doacao.local";

    private async Task<string> RegisterUserAsync()
    {
        var email = UniqueEmail();
        await RegisterUserAsync(await CreateAdminClientAsync(), email);
        return email;
    }

    private static async Task<Guid> RegisterUserAsync(HttpClient admin, string email)
    {
        var response = await admin.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = InitialPassword,
            fullName = "Usuária de Teste",
            role = "Admin",
            collectionPointId = (Guid?)null,
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<UserResponse>())!.Id;
    }

    private Task<HttpResponseMessage> RequestResetAsync(string email) =>
        _factory.CreateClient().PostAsJsonAsync("/api/auth/forgot-password", new { email });

    private Task<HttpResponseMessage> CompleteResetAsync(string token, string newPassword) =>
        _factory.CreateClient().PostAsJsonAsync("/api/auth/reset-password", new { token, newPassword });

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
}
