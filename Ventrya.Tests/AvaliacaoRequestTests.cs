
using System.ComponentModel.DataAnnotations;
using Ventrya.Api.DTOs;
using Xunit;

namespace Ventrya.Tests;

public class AvaliacaoRequestTests
{
    private static bool EhValido(AvaliacaoRequest request)
    {
        var resultados = new List<ValidationResult>();

        return Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            resultados,
            true);
    }

    [Fact]
    public void AvaliacaoComDadosValidos_DeveSerAceita()
    {
        var request = new AvaliacaoRequest
        {
            DesafioId = 1,
            ParticipanteId = 1,
            Nota = 9,
            Feedback = "Excelente desempenho."
        };

        Assert.True(EhValido(request));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public void NotaForaDoIntervalo_DeveSerRejeitada(int nota)
    {
        var request = new AvaliacaoRequest
        {
            DesafioId = 1,
            ParticipanteId = 1,
            Nota = nota,
            Feedback = "Feedback valido."
        };

        Assert.False(EhValido(request));
    }

    [Fact]
    public void FeedbackMuitoCurto_DeveSerRejeitado()
    {
        var request = new AvaliacaoRequest
        {
            DesafioId = 1,
            ParticipanteId = 1,
            Nota = 8,
            Feedback = "Oi"
        };

        Assert.False(EhValido(request));
    }

    [Fact]
    public void DesafioIdInvalido_DeveSerRejeitado()
    {
        var request = new AvaliacaoRequest
        {
            DesafioId = 0,
            ParticipanteId = 1,
            Nota = 8,
            Feedback = "Avaliacao concluida."
        };

        Assert.False(EhValido(request));
    }
}
