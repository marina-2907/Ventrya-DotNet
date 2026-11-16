
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Ventrya.Api.Data;
using Xunit;

namespace Ventrya.Tests;

public class IntegracaoApiTests
{
    // Credenciais utilizadas apenas nos testes automatizados.
    private const string SenhaTeste = "senha-exclusiva-de-teste";
    private const string SenhaParticipanteTeste = "senha-participante-teste";

    private static readonly string ChaveTeste = new string('K', 48);

    // Cria uma instância isolada da API para os testes.
    private sealed class ApiTesteFactory : WebApplicationFactory<Program>
    {
        // Banco SQLite temporário, mantido durante cada teste.
        private readonly SqliteConnection _conexao =
            new("Data Source=:memory:");

        public ApiTesteFactory()
        {
            _conexao.Open();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            // Configurações exclusivas dos testes.
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Jwt:Key"] = ChaveTeste,
                        ["DemoAuth:Password"] = SenhaTeste,
                        ["DemoAuth:ParticipantePassword"] =
                            SenhaParticipanteTeste
                    });
            });

            builder.ConfigureTestServices(services =>
            {
                // Substitui o SQLite real pelo banco em memória.
                services.RemoveAll<DbContextOptions<VentryaDbContext>>();

                services.AddDbContext<VentryaDbContext>(options =>
                    options.UseSqlite(_conexao));

                // Usa a mesma chave JWT para gerar e validar tokens.
                services.PostConfigure<JwtBearerOptions>(
                    JwtBearerDefaults.AuthenticationScheme,
                    options =>
                    {
                        options.TokenValidationParameters =
                            new TokenValidationParameters
                            {
                                ValidateIssuer = true,
                                ValidateAudience = true,
                                ValidateLifetime = true,
                                ValidateIssuerSigningKey = true,

                                ValidIssuer = "Ventrya.Api",
                                ValidAudience = "Ventrya.Client",

                                IssuerSigningKey =
                                    new SymmetricSecurityKey(
                                        Encoding.UTF8.GetBytes(ChaveTeste)),

                                ClockSkew = TimeSpan.Zero
                            };
                    });
            });
        }

        // Cria as tabelas no SQLite temporário.
        public async Task CriarBancoAsync()
        {
            using var scope = Services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<VentryaDbContext>();

            await db.Database.EnsureCreatedAsync();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _conexao.Dispose();
            }
        }
    }

    // Faz login e retorna o token JWT do perfil informado.
    private static async Task<string> FazerLoginAsync(
        HttpClient client,
        string usuario = "avaliador",
        string senha = SenhaTeste)
    {
        using var resposta = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                usuario,
                senha
            });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        using var json = JsonDocument.Parse(
            await resposta.Content.ReadAsStringAsync());

        var token = json.RootElement
            .GetProperty("token")
            .GetString();

        Assert.False(string.IsNullOrWhiteSpace(token));

        return token!;
    }

    // TESTE 1: Acesso sem token deve retornar 401.
    [Fact]
    public async Task SemToken_DeveRetornar401()
    {
        using var factory = new ApiTesteFactory();
        using var client = factory.CreateClient();

        using var resposta = await client.GetAsync("/api/avaliacoes");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            resposta.StatusCode);
    }

    // TESTE 2: Login válido deve gerar um token JWT.
    [Fact]
    public async Task LoginValido_DeveRetornarToken()
    {
        using var factory = new ApiTesteFactory();
        using var client = factory.CreateClient();

        var token = await FazerLoginAsync(client);

        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    // TESTE 3: POST repetido não pode criar duas avaliações.
    [Fact]
    public async Task PostRepetido_NaoDeveDuplicarAvaliacao()
    {
        using var factory = new ApiTesteFactory();
        using var client = factory.CreateClient();

        await factory.CriarBancoAsync();

        var token = await FazerLoginAsync(client);

        var dados = new
        {
            desafioId = 1,
            participanteId = 1,
            nota = 9,
            feedback = "Excelente desempenho no desafio."
        };

        // Envia a mesma requisição duas vezes.
        async Task<HttpResponseMessage> EnviarAsync()
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/avaliacoes");

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            request.Headers.Add(
                "Idempotency-Key",
                "teste-123");

            request.Content = JsonContent.Create(dados);

            return await client.SendAsync(request);
        }

        using var primeira = await EnviarAsync();
        using var segunda = await EnviarAsync();

        // As duas chamadas devem retornar HTTP 201.
        Assert.Equal(HttpStatusCode.Created, primeira.StatusCode);
        Assert.Equal(HttpStatusCode.Created, segunda.StatusCode);

        var avaliacao1 = await primeira.Content
            .ReadFromJsonAsync<JsonElement>();

        var avaliacao2 = await segunda.Content
            .ReadFromJsonAsync<JsonElement>();

        // As duas respostas devem ter o mesmo ID.
        Assert.Equal(
            avaliacao1.GetProperty("id").GetInt32(),
            avaliacao2.GetProperty("id").GetInt32());

        // Consulta as avaliações cadastradas.
        using var consulta = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/avaliacoes");

        consulta.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        using var respostaConsulta = await client.SendAsync(consulta);

        Assert.Equal(
            HttpStatusCode.OK,
            respostaConsulta.StatusCode);

        var avaliacoes = await respostaConsulta.Content
            .ReadFromJsonAsync<JsonElement>();

        // Apenas uma avaliação deve existir no banco.
        Assert.Equal(1, avaliacoes.GetArrayLength());
    }

    // TESTE 4: Participante não pode cadastrar avaliações.
    [Fact]
    public async Task Participante_NaoPodeCadastrarAvaliacao()
    {
        using var factory = new ApiTesteFactory();
        using var client = factory.CreateClient();

        // Faz login como participante.
        var token = await FazerLoginAsync(
            client,
            "participante",
            SenhaParticipanteTeste);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/avaliacoes");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        request.Headers.Add(
            "Idempotency-Key",
            "teste-permissao-participante");

        request.Content = JsonContent.Create(new
        {
            desafioId = 1,
            participanteId = 1,
            nota = 8,
            feedback = "Tentativa sem permissao."
        });

        using var resposta = await client.SendAsync(request);

        // Participante autenticado, mas sem autorização.
        Assert.Equal(
            HttpStatusCode.Forbidden,
            resposta.StatusCode);
    }


    // TESTE 5: Avaliação inválida deve retornar HTTP 400.
    [Fact]
    public async Task AvaliacaoInvalida_DeveRetornar400()
    {
        using var factory = new ApiTesteFactory();
        using var client = factory.CreateClient();

        var token = await FazerLoginAsync(client);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/avaliacoes");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        request.Headers.Add(
            "Idempotency-Key",
            "teste-avaliacao-invalida");

        request.Content = JsonContent.Create(new
        {
            desafioId = 1,
            participanteId = 1,
            nota = 15,
            feedback = "Nota fora do intervalo permitido."
        });

        using var resposta = await client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            resposta.StatusCode);
    }

    // TESTE 6: Mesma chave com dados diferentes retorna 409.
    [Fact]
    public async Task IdempotenciaComDadosDiferentes_DeveRetornar409()
    {
        using var factory = new ApiTesteFactory();
        using var client = factory.CreateClient();

        await factory.CriarBancoAsync();

        var token = await FazerLoginAsync(client);

        async Task<HttpResponseMessage> EnviarAsync(int nota)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/avaliacoes");

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            request.Headers.Add(
                "Idempotency-Key",
                "teste-conflito-409");

            request.Content = JsonContent.Create(new
            {
                desafioId = 1,
                participanteId = 1,
                nota,
                feedback = "Avaliacao com chave repetida."
            });

            return await client.SendAsync(request);
        }

        using var primeira = await EnviarAsync(8);
        using var segunda = await EnviarAsync(9);

        Assert.Equal(
            HttpStatusCode.Created,
            primeira.StatusCode);

        Assert.Equal(
            HttpStatusCode.Conflict,
            segunda.StatusCode);
    }

    // TESTE 7: Exceder o limite de chamadas retorna 429.
    [Fact]
    public async Task ExcessoDeRequisicoes_DeveRetornar429()
    {
        using var factory = new ApiTesteFactory();
        using var client = factory.CreateClient();

        await factory.CriarBancoAsync();

        var token = await FazerLoginAsync(client);

        // O limite configurado é de 5 chamadas por 30 segundos.
        for (int i = 0; i < 5; i++)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/avaliacoes");

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            using var resposta = await client.SendAsync(request);

            Assert.Equal(
                HttpStatusCode.OK,
                resposta.StatusCode);
        }

        // A sexta chamada deve ser bloqueada.
        using var ultimaRequisicao = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/avaliacoes");

        ultimaRequisicao.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        using var ultimaResposta =
            await client.SendAsync(ultimaRequisicao);

        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            ultimaResposta.StatusCode);
    }

    // TESTE 8: Health Check deve retornar HTTP 200.
    [Fact]
    public async Task HealthCheck_DeveRetornar200()
    {
        using var factory = new ApiTesteFactory();
        using var client = factory.CreateClient();

        await factory.CriarBancoAsync();

        using var resposta = await client.GetAsync("/health");

        Assert.Equal(
            HttpStatusCode.OK,
            resposta.StatusCode);

        var conteudo = await resposta.Content.ReadAsStringAsync();

        Assert.Contains("Healthy", conteudo);
    }

}
