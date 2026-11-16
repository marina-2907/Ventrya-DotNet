
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Ventrya.Api.Data;
using Ventrya.Api.DTOs;
using Ventrya.Api.Models;

namespace Ventrya.Api.Endpoints;

public static class AvaliacaoEndpoints
{
    // Evita execuções simultâneas na mesma instância da API.
    private static readonly SemaphoreSlim Controle = new(1, 1);

    public static void MapAvaliacaoEndpoints(this WebApplication app)
    {
        var grupo = app.MapGroup("/api/avaliacoes")
            .WithTags("Avaliações")
            .RequireAuthorization()
            .RequireRateLimiting("limite-api");

        // GET - Listar avaliações.
        grupo.MapGet("/", async (VentryaDbContext db) =>
        {
            return Results.Ok(
                await db.Avaliacoes.AsNoTracking().ToListAsync());
        });

        // GET - Buscar avaliação por ID.
        grupo.MapGet("/{id:int}", async (
            int id, VentryaDbContext db) =>
        {
            var avaliacao = await db.Avaliacoes
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);

            return avaliacao is null
                ? Results.NotFound()
                : Results.Ok(avaliacao);
        });

        // POST - Criar avaliação com idempotência.
        grupo.MapPost("/", async (
            HttpContext context,
            AvaliacaoRequest request,
            VentryaDbContext db) =>
        {
            var chave = context.Request.Headers[
                "Idempotency-Key"].ToString();

            if (string.IsNullOrWhiteSpace(chave) ||
                chave.Length > 100)
            {
                return Results.BadRequest(new
                {
                    mensagem = "Informe Idempotency-Key (até 100 caracteres)."
                });
            }

            var erro = Validar(request);

            if (erro is not null)
                return Results.BadRequest(new { mensagem = erro });

            // Associa a chave ao usuário autenticado.
            var usuario = context.User.Identity?.Name ?? "anonimo";
            var chaveCompleta = $"{usuario}:{chave}";

            // Calcula uma assinatura dos dados enviados.
            var conteudo = JsonSerializer.Serialize(new
            {
                request.DesafioId,
                request.ParticipanteId,
                request.Nota,
                Feedback = request.Feedback.Trim()
            });

            var hash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(conteudo)));

            await Controle.WaitAsync();

            try
            {
                var anterior = await db.RegistrosIdempotencia
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Chave == chaveCompleta);

                if (anterior is not null)
                {
                    // Mesma chave com dados diferentes: conflito.
                    if (anterior.HashRequisicao != hash)
                        return Results.Conflict(new
                        {
                            mensagem = "Chave já utilizada com outros dados."
                        });

                    // Mesma chave e mesmos dados:
                    // devolve a resposta original, sem duplicar.
                    context.Response.Headers.Location =
                        anterior.Localizacao;

                    return Results.Content(
                        anterior.RespostaJson,
                        contentType: "application/json",
                        statusCode: StatusCodes.Status201Created);
                }

                // A avaliação e a chave serão salvas
                // na mesma transação do banco.
                await using var transacao =
                    await db.Database.BeginTransactionAsync();

                var avaliacao = new Avaliacao
                {
                    DesafioId = request.DesafioId,
                    ParticipanteId = request.ParticipanteId,
                    Nota = request.Nota,
                    Feedback = request.Feedback.Trim()
                };

                db.Avaliacoes.Add(avaliacao);
                await db.SaveChangesAsync();

                var localizacao = $"/api/avaliacoes/{avaliacao.Id}";

                var respostaJson = JsonSerializer.Serialize(
                    avaliacao,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web));

                db.RegistrosIdempotencia.Add(
                    new RegistroIdempotencia
                    {
                        Chave = chaveCompleta,
                        HashRequisicao = hash,
                        RespostaJson = respostaJson,
                        Localizacao = localizacao,
                        CriadoEmUtc = DateTime.UtcNow
                    });

                await db.SaveChangesAsync();
                await transacao.CommitAsync();

                return Results.Created(localizacao, avaliacao);
            }
            finally
            {
                Controle.Release();
            }
        })
        .RequireAuthorization("Avaliador");

        // PUT - Atualizar avaliação.
        grupo.MapPut("/{id:int}", async (
            int id,
            AvaliacaoRequest request,
            VentryaDbContext db) =>
        {
            var avaliacao = await db.Avaliacoes.FindAsync(id);

            if (avaliacao is null)
                return Results.NotFound();

            var erro = Validar(request);

            if (erro is not null)
                return Results.BadRequest(new { mensagem = erro });

            avaliacao.DesafioId = request.DesafioId;
            avaliacao.ParticipanteId = request.ParticipanteId;
            avaliacao.Nota = request.Nota;
            avaliacao.Feedback = request.Feedback.Trim();

            await db.SaveChangesAsync();

            return Results.Ok(avaliacao);
        })
        .RequireAuthorization("Avaliador");

        // DELETE - Excluir avaliação.
        grupo.MapDelete("/{id:int}", async (
            int id, VentryaDbContext db) =>
        {
            var avaliacao = await db.Avaliacoes.FindAsync(id);

            if (avaliacao is null)
                return Results.NotFound();

            db.Avaliacoes.Remove(avaliacao);
            await db.SaveChangesAsync();

            return Results.NoContent();
        })
        .RequireAuthorization("Avaliador");
    }

    private static string? Validar(AvaliacaoRequest request)
    {
        var resultados = new List<ValidationResult>();
        var contexto = new ValidationContext(request);

        var valido = Validator.TryValidateObject(
            request, contexto, resultados, true);

        if (!valido)
            return resultados.First().ErrorMessage;

        if (string.IsNullOrWhiteSpace(request.Feedback))
            return "O feedback não pode estar vazio.";

        return null;
    }
}
