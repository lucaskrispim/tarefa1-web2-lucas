// Lucas Wilman da Silva Crispim
// ViewModel da tela de exclusão de profissional pelo gerente, mostrando os pacientes
// vinculados e indicando quais são exclusivos (serão removidos na exclusão em cascata).
#nullable disable
using System.Collections.Generic;
using System.Linq;

namespace Projeto1_Web2_IF_Lucas.Models.ViewModels
{
    public class ExcluirProfissionalViewModel
    {
        public TbProfissional Profissional { get; set; }

        public List<PacienteVinculoItem> Pacientes { get; set; } = new();

        public bool TemPacientes => Pacientes.Count > 0;

        public int QtdExclusivos => Pacientes.Count(p => p.Exclusivo);

        // Exclusivo = paciente vinculado apenas a este profissional (será removido na cascata).
        public record PacienteVinculoItem(int IdPaciente, string Nome, bool Exclusivo);
    }
}
