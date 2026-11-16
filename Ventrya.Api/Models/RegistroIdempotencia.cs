
namespace Ventrya.Api.Models;

public class RegistroIdempotencia
{
    public string Chave { get; set; } = string.Empty;
    public string HashRequisicao { get; set; } = string.Empty;
    public string RespostaJson { get; set; } = string.Empty;
    public string Localizacao { get; set; } = string.Empty;
    public DateTime CriadoEmUtc { get; set; }
}
