using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projeto1_Web2_IF_Lucas.Models.ViewModels;
using Projeto1_Web2_IF_Lucas.Services;

// Lucas Wilman da Silva Crispim
namespace Projeto1_Web2_IF_Lucas.Controllers
{
    // Auto-cadastro do profissional (Médico ou Nutricionista). É a única tela que junta login +
    // senha + dados do profissional. A lógica fica em IRegistroService; o controller cuida do
    // HTTP (ModelState, autenticação e redirecionamentos). Gerentes NÃO se cadastram aqui.
    public class ContaController : Controller
    {
        private readonly IRegistroService _registro;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<ContaController> _logger;

        public ContaController(
            IRegistroService registro,
            SignInManager<IdentityUser> signInManager,
            ILogger<ContaController> logger)
        {
            _registro = registro;
            _signInManager = signInManager;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Registrar()
        {
            var vm = new RegistrarProfissionalViewModel();
            await _registro.CarregarOpcoesAsync(vm);
            return View(vm);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(RegistrarProfissionalViewModel vm)
        {
            // Falhas de validação por DataAnnotations (e-mail, senha, campos obrigatórios).
            if (!ModelState.IsValid)
            {
                await _registro.CarregarOpcoesAsync(vm);
                return View(vm);
            }

            var resultado = await _registro.RegistrarAsync(vm);
            if (!resultado.Sucesso)
            {
                foreach (var erro in resultado.Erros)
                {
                    ModelState.AddModelError(erro.Campo, erro.Mensagem);
                }
                await _registro.CarregarOpcoesAsync(vm);
                return View(vm);
            }

            // A conta já está criada e válida; se o sign-in automático falhar, vai para o login.
            try
            {
                await _signInManager.SignInAsync(resultado.Usuario!, isPersistent: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cadastro concluído, mas falhou o login automático de {Email}.", vm.Email);
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
