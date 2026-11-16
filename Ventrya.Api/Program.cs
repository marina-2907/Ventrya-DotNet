
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Ventrya.Api.Data;
using Ventrya.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Documentação OpenAPI
builder.Services.AddOpenApi();


builder.Services.AddHealthChecks()
    .AddDbContextCheck<VentryaDbContext>("banco-sqlite");


// Banco de dados SQLite
builder.Services.AddDbContext<VentryaDbContext>(options =>
    options.UseSqlite("Data Source=ventrya.db"));

// Chave JWT armazenada no User Secrets
var chaveJwt = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(chaveJwt) ||
    Encoding.UTF8.GetByteCount(chaveJwt) < 32)
{
    throw new InvalidOperationException(
        "Configure Jwt:Key com pelo menos 32 bytes usando User Secrets.");
}

// Autenticação JWT
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = "Ventrya.Api",
            ValidAudience = "Ventrya.Client",

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(chaveJwt)),

            ClockSkew = TimeSpan.Zero
        };
    });

// Autorização por perfil
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Avaliador", policy =>
        policy.RequireRole("Avaliador"));

// RATE LIMITING
// Permite até 5 requisições a cada 30 segundos
// nas rotas que utilizarem esta política.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;

    options.AddPolicy("limite-api", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name
                ?? context.Connection.RemoteIpAddress?.ToString()
                ?? "anonimo",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromSeconds(30),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/openapi/v1.json",
            "Ventrya API v1");
    });
}


// Middlewares de segurança
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

// Informações da plataforma
app.MapGet("/", () => Results.Ok(new
{
    projeto = "Ventrya",
    modulo = "Avaliacoes e Competencias",
    status = "API funcionando!",
    versao = "1.0.0"
}))
.WithTags("Sistema");

// Rotas de autenticação e avaliações
app.MapAuthEndpoints();
app.MapAvaliacaoEndpoints();

app.MapHealthChecks("/health");

app.Run();

public partial class Program { }