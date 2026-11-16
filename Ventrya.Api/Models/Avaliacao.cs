
namespace Ventrya.Api.Models;

// Representa a avaliação de um desafio prático.
public class Avaliacao
{
    public int Id { get; set; }

    public int DesafioId { get; set; }

    public int ParticipanteId { get; set; }

    public int Nota { get; set; }

    public string Feedback { get; set; } = string.Empty;
}
