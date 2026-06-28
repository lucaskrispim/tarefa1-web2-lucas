using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projeto1_Web2_IF_Lucas.Data;
using Projeto1_Web2_IF_Lucas.Models;
using Projeto1_Web2_IF_Lucas.Services;

// Lucas Wilman da Silva Crispim
namespace Projeto1_Web2_IF_Lucas.Controllers
{
    // CRUD de pacientes pelo PRÓPRIO profissional (Médico ou Nutricionista). A lógica de
    // negócio/persistência fica em IPacienteService; o controller cuida do HTTP (autorização,
    // ModelState, binding e mensagens). O ownership é garantido pelo serviço (via tbMedico_Paciente).
    [Authorize(Roles = DbInitializer.RoleMedico + "," + DbInitializer.RoleNutricionista)]
    public class MeusPacientesController : Controller
    {
        private readonly IPacienteService _pacientes;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<MeusPacientesController> _logger;

        public MeusPacientesController(
            IPacienteService pacientes,
            UserManager<IdentityUser> userManager,
            ILogger<MeusPacientesController> logger)
        {
            _pacientes = pacientes;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var idProfissional = await ObterIdProfissionalAtualAsync();
            if (idProfissional == null)
            {
                return Forbid();
            }
            return View(await _pacientes.ListarDoProfissionalAsync(idProfissional.Value));
        }

        public async Task<IActionResult> Detalhes(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var idProfissional = await ObterIdProfissionalAtualAsync();
            if (idProfissional == null)
            {
                return Forbid();
            }

            var paciente = await _pacientes.ObterDoProfissionalAsync(id.Value, idProfissional.Value, comNavegacoes: true);
            if (paciente == null)
            {
                return NotFound();
            }

            ViewData["InformacaoResumida"] = await _pacientes.ObterInformacaoResumidaAsync(id.Value, idProfissional.Value);
            return View(paciente);
        }

        public async Task<IActionResult> Criar()
        {
            await PreencherCidadesAsync(null);
            return View(new TbPaciente());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(
            [Bind("Nome,Rg,Cpf,DataNascimento,NomeResponsavel,Sexo,Etnia,Endereco,Bairro,IdCidade,TelResidencial,TelCelular,Profissao,FlgAtleta,FlgGestante")] TbPaciente paciente,
            string? informacaoResumida)
        {
            var idProfissional = await ObterIdProfissionalAtualAsync();
            if (idProfissional == null)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _pacientes.CriarAsync(paciente, idProfissional.Value, informacaoResumida);
                    TempData["Mensagem"] = "Paciente cadastrado com sucesso.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Falha ao cadastrar paciente para o profissional {IdProfissional}.",
                        idProfissional);
                    ModelState.AddModelError(string.Empty,
                        "Não foi possível salvar o paciente. Tente novamente e, se persistir, " +
                        "contate o administrador do sistema.");
                }
            }

            await PreencherCidadesAsync(paciente.IdCidade);
            ViewData["InformacaoResumida"] = informacaoResumida;
            return View(paciente);
        }

        public async Task<IActionResult> Editar(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var idProfissional = await ObterIdProfissionalAtualAsync();
            if (idProfissional == null)
            {
                return Forbid();
            }

            var paciente = await _pacientes.ObterDoProfissionalAsync(id.Value, idProfissional.Value, comNavegacoes: false);
            if (paciente == null)
            {
                return NotFound();
            }

            await PreencherCidadesAsync(paciente.IdCidade);
            ViewData["InformacaoResumida"] = await _pacientes.ObterInformacaoResumidaAsync(id.Value, idProfissional.Value);
            return View(paciente);
        }

        // TryUpdateModelAsync (sem IdPaciente) evita overposting; a persistência fica no serviço.
        [HttpPost, ActionName("Editar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarConfirmado(int id, string? informacaoResumida)
        {
            var idProfissional = await ObterIdProfissionalAtualAsync();
            if (idProfissional == null)
            {
                return Forbid();
            }

            var paciente = await _pacientes.ObterDoProfissionalAsync(id, idProfissional.Value, comNavegacoes: false);
            if (paciente == null)
            {
                return NotFound();
            }

            var atualizou = await TryUpdateModelAsync(
                paciente,
                string.Empty,
                p => p.Nome, p => p.Rg, p => p.Cpf, p => p.DataNascimento, p => p.NomeResponsavel,
                p => p.Sexo, p => p.Etnia, p => p.Endereco, p => p.Bairro, p => p.IdCidade,
                p => p.TelResidencial, p => p.TelCelular,
                p => p.Profissao, p => p.FlgAtleta, p => p.FlgGestante);

            if (atualizou && ModelState.IsValid)
            {
                try
                {
                    await _pacientes.SalvarEdicaoAsync(paciente, idProfissional.Value, informacaoResumida);
                    TempData["Mensagem"] = "Paciente atualizado com sucesso.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Falha ao editar o paciente {IdPaciente}.", id);
                    ModelState.AddModelError(string.Empty,
                        "Não foi possível salvar as alterações. Tente novamente e, se persistir, " +
                        "contate o administrador do sistema.");
                }
            }

            await PreencherCidadesAsync(paciente.IdCidade);
            ViewData["InformacaoResumida"] = informacaoResumida;
            return View(paciente);
        }

        public async Task<IActionResult> Excluir(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var idProfissional = await ObterIdProfissionalAtualAsync();
            if (idProfissional == null)
            {
                return Forbid();
            }

            var paciente = await _pacientes.ObterDoProfissionalAsync(id.Value, idProfissional.Value, comNavegacoes: true);
            if (paciente == null)
            {
                return NotFound();
            }
            return View(paciente);
        }

        [HttpPost, ActionName("Excluir")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExcluirConfirmado(int id)
        {
            var idProfissional = await ObterIdProfissionalAtualAsync();
            if (idProfissional == null)
            {
                return Forbid();
            }

            try
            {
                await _pacientes.ExcluirDoProfissionalAsync(id, idProfissional.Value);
                TempData["Mensagem"] = "Paciente removido com sucesso.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Falha ao excluir o paciente {IdPaciente}.", id);
                TempData["Erro"] = "Não foi possível excluir o paciente. Tente novamente e, se persistir, " +
                    "contate o administrador do sistema.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<int?> ObterIdProfissionalAtualAsync()
            => await _pacientes.ObterIdProfissionalPorUsuarioAsync(_userManager.GetUserId(User)!);

        private async Task PreencherCidadesAsync(int? selecionada)
            => ViewData["IdCidade"] = await _pacientes.ObterCidadesAsync(selecionada);
    }
}
