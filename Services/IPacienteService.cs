using Microsoft.AspNetCore.Mvc.Rendering;
using Projeto1_Web2_IF_Lucas.Models;

// Lucas Wilman da Silva Crispim
namespace Projeto1_Web2_IF_Lucas.Services
{
    // Regras de negócio dos pacientes do próprio profissional (ownership via tbMedico_Paciente).
    public interface IPacienteService
    {
        Task<int?> ObterIdProfissionalPorUsuarioAsync(string userId);

        Task<SelectList> ObterCidadesAsync(int? selecionada);

        Task<List<TbPaciente>> ListarDoProfissionalAsync(int idProfissional);

        Task<TbPaciente?> ObterDoProfissionalAsync(int idPaciente, int idProfissional, bool comNavegacoes);

        Task<string?> ObterInformacaoResumidaAsync(int idPaciente, int idProfissional);

        // Cria o paciente e o vínculo com o profissional (atômico). Lança DbUpdateException em falha.
        Task CriarAsync(TbPaciente paciente, int idProfissional, string? informacaoResumida);

        // Persiste a edição do paciente (já carregado/alterado pelo controller) e a anotação do vínculo.
        Task SalvarEdicaoAsync(TbPaciente paciente, int idProfissional, string? informacaoResumida);

        // Remove o vínculo do profissional e o paciente caso fique órfão. False se não for dele.
        Task<bool> ExcluirDoProfissionalAsync(int idPaciente, int idProfissional);
    }
}
