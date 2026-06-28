using Microsoft.AspNetCore.Mvc.Rendering;
using Projeto1_Web2_IF_Lucas.Models;

// Lucas Wilman da Silva Crispim
namespace Projeto1_Web2_IF_Lucas.Services
{
    // Regras de negócio dos profissionais: perfil próprio (por IdUser) e administração pelos
    // gerentes (listagem por escopo, edição, exclusão com cascata).
    public interface IProfissionalService
    {
        Task<SelectList> ObterCidadesAsync(int? selecionada);

        // ---- Perfil do próprio profissional (MeuPerfil) ----
        Task<TbProfissional?> ObterPorUsuarioAsync(string userId, bool comNavegacoes);

        // ---- Administração pelos gerentes ----
        Task<List<TbProfissional>> ListarPorEscopoAsync(int? tipoPermitido);
        Task<TbProfissional?> ObterAsync(int id, bool comNavegacoes);
        Task<List<PacienteVinculoInfo>> ObterPacientesVinculadosAsync(int idProfissional);

        // Persiste alterações já aplicadas (TryUpdateModelAsync) ao profissional rastreado,
        // mantendo a string Cidade coerente com IdCidade. Lança DbUpdateException em falha.
        Task SalvarComCidadeAsync(TbProfissional profissional);

        // Exclui o profissional + contrato + login. Com cascata, remove também os pacientes
        // exclusivos (vinculados só a ele); compartilhados são preservados.
        Task<ExclusaoResultado> ExcluirAsync(int id, bool cascata);
    }

    // Paciente vinculado a um profissional, com a marca de "exclusivo" (só deste profissional).
    public record PacienteVinculoInfo(int IdPaciente, string Nome, bool Exclusivo);
}
