
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Ventrya.Api.Endpoints;

public record LoginRequest(string Usuario, string Senha);

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/api/auth/login", (
            LoginRequest request,
            IConfiguration config) =>
        {
            string? senhaConfigurada;
            string perfil;

            if (request.Usuario == "avaliador")
            {
                senhaConfigurada = config["DemoAuth:Password"];
                perfil = "Avaliador";
            }
            else if (request.Usuario == "participante")
            {
                senhaConfigurada = config["DemoAuth:ParticipantePassword"];
                perfil = "Participante";
            }
            else
            {
                return Results.Unauthorized();
            }

            if (string.IsNullOrEmpty(senhaConfigurada))
                return Results.Problem(
                    "Credenciais de demonstração não configuradas.",
                    statusCode: 500);

            var senhaValida = CryptographicOperations.FixedTimeEquals(
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(request.Senha ?? "")),
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(senhaConfigurada)));

            if (!senhaValida)
                return Results.Unauthorized();

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, request.Usuario),
                new Claim(ClaimTypes.Role, perfil),
                new Claim(JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString())
            };

            var chave = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(config["Jwt:Key"]!));

            var credenciais = new SigningCredentials(
                chave, SecurityAlgorithms.HmacSha256);

            var expiracao = DateTime.UtcNow.AddMinutes(30);

            var token = new JwtSecurityToken(
                issuer: "Ventrya.Api",
                audience: "Ventrya.Client",
                claims: claims,
                expires: expiracao,
                signingCredentials: credenciais);

            return Results.Ok(new
            {
                token = new JwtSecurityTokenHandler()
                    .WriteToken(token),
                tipo = "Bearer",
                perfil,
                expiraEm = expiracao
            });
        })
        .WithTags("Autenticação");
    }
}
