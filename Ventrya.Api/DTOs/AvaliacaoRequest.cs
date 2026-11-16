
using System.ComponentModel.DataAnnotations;

namespace Ventrya.Api.DTOs;

// Dados recebidos para cadastrar ou atualizar uma avaliação.
public class AvaliacaoRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "O ID do desafio deve ser positivo.")]
    public int DesafioId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "O ID do participante deve ser positivo.")]
    public int ParticipanteId { get; set; }

    [Range(0, 10, ErrorMessage = "A nota deve estar entre 0 e 10.")]
    public int Nota { get; set; }

    [Required(ErrorMessage = "O feedback e obrigatorio.")]
    [MinLength(5, ErrorMessage = "O feedback deve ter pelo menos 5 caracteres.")]
    public string Feedback { get; set; } = string.Empty;
}
