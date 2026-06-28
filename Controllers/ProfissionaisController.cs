using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projeto1_Web2_IF_Lucas.Data;
using Projeto1_Web2_IF_Lucas.Models;
using Projeto1_Web2_IF_Lucas.Models.ViewModels;
using Projeto1_Web2_IF_Lucas.Services;

// Lucas Wilman da Silva Crispim
namespace Projeto1_Web2_IF_Lucas.Controllers
{
    // Área dos GERENTES. O controller cuida da autorização/escopo por Role (concern web) e
    // delega a lógica de dados/negócio ao IProfissionalService. Cada gerente só acessa o seu
    // escopo (GerenteMedico->médicos, GerenteNutricionista->nutris, GerenteGeral->todos),
    // validado no servidor em toda action com id. Gerentes não criam profissionais.
    [Authorize(Roles = DbInitializer.RoleGerenteMedico + "," +
                       DbInitializer.RoleGerenteNutricionista + "," +
                       DbInitializer.RoleGerenteGeral)]
    public class ProfissionaisController : Controller
    {
        private readonly IProfissionalService _profissionais;

        public ProfissionaisController(IProfissionalService profissionais)
        {
            _profissionais = profissionais;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _profissionais.ListarPorEscopoAsync(TipoPermitido()));
        }

        public async Task<IActionResult> Detalhes(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var profissional = await _profissionais.ObterAsync(id.Value, comNavegacoes: true);
            if (profissional == null)
            {
                return NotFound();
            }
            if (!DentroDoEscopo(profissional))
            {
                return Forbid();
            }
            return View(profissional);
        }

        public async Task<IActionResult> Editar(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var profissional = await _profissionais.ObterAsync(id.Value, comNavegacoes: false);
            if (profissional == null)
            {
                return NotFound();
            }
            if (!DentroDoEscopo(profissional))
            {
                return Forbid();
            }
            await PreencherCidadesAsync(profissional.IdCidade);
            return View(profissional);
        }

        // O gerente PODE alterar o CPF (diferente do auto-cadastro).
        [HttpPost, ActionName("Editar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarConfirmado(int id)
        {
            var profissional = await _profissionais.ObterAsync(id, comNavegacoes: false);
            if (profissional == null)
            {
                return NotFound();
            }
            if (!DentroDoEscopo(profissional))
            {
                return Forbid();
            }

            var atualizou = await TryUpdateModelAsync(
                profissional,
                string.Empty,
                p => p.Nome, p => p.Cpf, p => p.CrmCrn, p => p.Especialidade,
                p => p.Logradouro, p => p.Numero, p => p.Bairro, p => p.Cep,
                p => p.Estado, p => p.IdCidade,
                p => p.Ddd1, p => p.Telefone1, p => p.Salario);

            if (atualizou && ModelState.IsValid)
            {
                try
                {
                    await _profissionais.SalvarComCidadeAsync(profissional);
                    TempData["Mensagem"] = "Profissional atualizado com sucesso.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError(string.Empty,
                        "Não foi possível salvar as alterações. Tente novamente e, se persistir, " +
                        "contate o administrador do sistema.");
                }
            }

            await PreencherCidadesAsync(profissional.IdCidade);
            return View(profissional);
        }

        public async Task<IActionResult> Excluir(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var profissional = await _profissionais.ObterAsync(id.Value, comNavegacoes: true);
            if (profissional == null)
            {
                return NotFound();
            }
            if (!DentroDoEscopo(profissional))
            {
                return Forbid();
            }

            var pacientes = await _profissionais.ObterPacientesVinculadosAsync(id.Value);
            var vm = new ExcluirProfissionalViewModel
            {
                Profissional = profissional,
                Pacientes = pacientes
                    .Select(p => new ExcluirProfissionalViewModel.PacienteVinculoItem(
                        p.IdPaciente, p.Nome, p.Exclusivo))
                    .ToList()
            };
            return View(vm);
        }

        // cascata=true remove também os pacientes exclusivos do profissional.
        [HttpPost, ActionName("Excluir")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExcluirConfirmado(int id, bool cascata = false)
        {
            // Valida o escopo antes de excluir (não confiar só no Index/na tela).
            var profissional = await _profissionais.ObterAsync(id, comNavegacoes: false);
            if (profissional == null)
            {
                return RedirectToAction(nameof(Index));
            }
            if (!DentroDoEscopo(profissional))
            {
                return Forbid();
            }

            try
            {
                var resultado = await _profissionais.ExcluirAsync(id, cascata);
                switch (resultado.Status)
                {
                    case StatusExclusao.BloqueadoComPacientes:
                        TempData["Erro"] = "Não é possível excluir: o profissional possui pacientes. " +
                            "Use a exclusão em cascata para removê-lo junto com os pacientes exclusivos.";
                        return RedirectToAction(nameof(Excluir), new { id });

                    case StatusExclusao.SucessoComAvisoLogin:
                        TempData["Erro"] = "Profissional excluído, mas a conta de login associada " +
                            "precisa ser removida manualmente.";
                        return RedirectToAction(nameof(Index));

                    case StatusExclusao.Sucesso:
                        TempData["Mensagem"] = resultado.PacientesExclusivosRemovidos > 0
                            ? $"Profissional excluído com sucesso, junto de {resultado.PacientesExclusivosRemovidos} paciente(s) exclusivo(s)."
                            : "Profissional excluído com sucesso.";
                        return RedirectToAction(nameof(Index));

                    default:
                        return RedirectToAction(nameof(Index));
                }
            }
            catch (DbUpdateException)
            {
                TempData["Erro"] = "Não foi possível excluir. Tente novamente e, se persistir, contate o administrador.";
                return RedirectToAction(nameof(Excluir), new { id });
            }
        }

        // Tipo de profissional que a role do gerente atual pode acessar (null = todos).
        private int? TipoPermitido()
        {
            if (User.IsInRole(DbInitializer.RoleGerenteGeral)) return null;
            if (User.IsInRole(DbInitializer.RoleGerenteMedico)) return DbInitializer.TipoMedico;
            if (User.IsInRole(DbInitializer.RoleGerenteNutricionista)) return DbInitializer.TipoNutricionista;
            // Sob [Authorize] isto não deve ocorrer; por segurança, não libera nada.
            return -1;
        }

        private bool DentroDoEscopo(TbProfissional profissional)
        {
            var tipoPermitido = TipoPermitido();
            return tipoPermitido == null || profissional.IdTipoProfissional == tipoPermitido;
        }

        private async Task PreencherCidadesAsync(int? selecionada)
            => ViewData["IdCidade"] = await _profissionais.ObterCidadesAsync(selecionada);
    }
}
