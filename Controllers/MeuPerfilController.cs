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
    // Área do próprio profissional: só VISUALIZA (Detalhes) e EDITA (Editar) os seus dados.
    // As actions operam sobre o registro do usuário logado (sem id), então nunca acessam dados
    // de outro profissional. O CPF não é editável (fica fora do TryUpdateModelAsync).
    [Authorize(Roles = DbInitializer.RoleMedico + "," + DbInitializer.RoleNutricionista)]
    public class MeuPerfilController : Controller
    {
        private readonly IProfissionalService _profissionais;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<MeuPerfilController> _logger;

        public MeuPerfilController(
            IProfissionalService profissionais,
            UserManager<IdentityUser> userManager,
            ILogger<MeuPerfilController> logger)
        {
            _profissionais = profissionais;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Detalhes()
        {
            var profissional = await _profissionais.ObterPorUsuarioAsync(_userManager.GetUserId(User)!, comNavegacoes: true);
            if (profissional == null)
            {
                return NotFound();
            }
            return View(profissional);
        }

        public async Task<IActionResult> Editar()
        {
            var profissional = await _profissionais.ObterPorUsuarioAsync(_userManager.GetUserId(User)!, comNavegacoes: false);
            if (profissional == null)
            {
                return NotFound();
            }
            await PreencherCidadesAsync(profissional.IdCidade);
            return View(profissional);
        }

        // CPF fica fora da lista (não editável pelo próprio profissional).
        [HttpPost, ActionName("Editar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarConfirmado()
        {
            var profissional = await _profissionais.ObterPorUsuarioAsync(_userManager.GetUserId(User)!, comNavegacoes: false);
            if (profissional == null)
            {
                return NotFound();
            }

            var atualizou = await TryUpdateModelAsync(
                profissional,
                string.Empty,
                p => p.Nome, p => p.CrmCrn, p => p.Especialidade,
                p => p.Logradouro, p => p.Numero, p => p.Bairro, p => p.Cep,
                p => p.Estado, p => p.IdCidade,
                p => p.Ddd1, p => p.Telefone1);

            if (atualizou && ModelState.IsValid)
            {
                try
                {
                    await _profissionais.SalvarComCidadeAsync(profissional);
                    TempData["Mensagem"] = "Dados atualizados com sucesso.";
                    return RedirectToAction(nameof(Detalhes));
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Falha ao salvar a edição do profissional {IdProfissional}.",
                        profissional.IdProfissional);
                    ModelState.AddModelError(string.Empty,
                        "Não foi possível salvar as alterações. Tente novamente e, se persistir, " +
                        "contate o administrador do sistema.");
                }
            }

            await PreencherCidadesAsync(profissional.IdCidade);
            return View(profissional);
        }

        private async Task PreencherCidadesAsync(int? selecionada)
            => ViewData["IdCidade"] = await _profissionais.ObterCidadesAsync(selecionada);
    }
}
