using Microsoft.AspNetCore.Identity;

// Lucas Wilman da Silva Crispim
namespace Projeto1_Web2_IF_Lucas.Services
{
    // Tipos de resultado usados pela camada de serviço para comunicar sucesso/falha
    // aos controllers sem depender de detalhes web (ModelState, View, etc.).

    public record ErroValidacao(string Campo, string Mensagem);

    public class RegistroResultado
    {
        public bool Sucesso { get; init; }
        public IdentityUser? Usuario { get; init; }
        public IReadOnlyList<ErroValidacao> Erros { get; init; } = new List<ErroValidacao>();

        public static RegistroResultado Ok(IdentityUser usuario) =>
            new() { Sucesso = true, Usuario = usuario };

        public static RegistroResultado Falha(IEnumerable<ErroValidacao> erros) =>
            new() { Sucesso = false, Erros = erros.ToList() };

        public static RegistroResultado Falha(string campo, string mensagem) =>
            Falha(new[] { new ErroValidacao(campo, mensagem) });
    }

    public enum StatusExclusao
    {
        Sucesso,
        BloqueadoComPacientes,
        SucessoComAvisoLogin,
        NaoEncontrado
    }

    public class ExclusaoResultado
    {
        public StatusExclusao Status { get; init; }
        public int PacientesExclusivosRemovidos { get; init; }

        public static ExclusaoResultado De(StatusExclusao status, int exclusivos = 0) =>
            new() { Status = status, PacientesExclusivosRemovidos = exclusivos };
    }
}
