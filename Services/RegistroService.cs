using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Projeto1_Web2_IF_Lucas.Data;
using Projeto1_Web2_IF_Lucas.Models;
using Projeto1_Web2_IF_Lucas.Models.ViewModels;

// Lucas Wilman da Silva Crispim
namespace Projeto1_Web2_IF_Lucas.Services
{
    public class RegistroService : IRegistroService
    {
        private readonly db_ifContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<RegistroService> _logger;

        public RegistroService(
            db_ifContext context,
            UserManager<IdentityUser> userManager,
            ILogger<RegistroService> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task CarregarOpcoesAsync(RegistrarProfissionalViewModel vm)
        {
            vm.Cidades = await _context.CidadesSelectListAsync(vm.IdCidade);

            vm.Tipos = await _context.TbTipoProfissional
                .OrderBy(t => t.IdTipoProfissional)
                .Select(t => new RegistrarProfissionalViewModel.TipoOption(t.IdTipoProfissional, t.Nome))
                .ToListAsync();

            var planos = await _context.TbPlano.OrderBy(p => p.Nome).ToListAsync();
            vm.Planos = planos
                .Select(p => new RegistrarProfissionalViewModel.PlanoOption(
                    p.IdPlano, p.Nome, p.Valor, TipoDoPlano(p.Nome)))
                .ToList();
        }

        public async Task<RegistroResultado> RegistrarAsync(RegistrarProfissionalViewModel vm)
        {
            var erros = new List<ErroValidacao>();

            var tipo = await _context.TbTipoProfissional
                .FirstOrDefaultAsync(t => t.IdTipoProfissional == vm.IdTipoProfissional);
            if (tipo == null)
            {
                erros.Add(new ErroValidacao(nameof(vm.IdTipoProfissional), "Tipo de profissional inválido."));
            }

            var plano = await _context.TbPlano.FirstOrDefaultAsync(p => p.IdPlano == vm.IdPlano);
            if (plano == null)
            {
                erros.Add(new ErroValidacao(nameof(vm.IdPlano), "Plano inválido."));
            }
            else if (TipoDoPlano(plano.Nome) != vm.IdTipoProfissional)
            {
                erros.Add(new ErroValidacao(nameof(vm.IdPlano),
                    "O plano selecionado não corresponde ao tipo de profissional."));
            }

            if (!await _context.TbCidade.AnyAsync(c => c.IdCidade == vm.IdCidade))
            {
                erros.Add(new ErroValidacao(nameof(vm.IdCidade), "Cidade inválida."));
            }

            if (!string.IsNullOrWhiteSpace(vm.Cpf) &&
                await _context.TbProfissional.AnyAsync(p => p.Cpf == vm.Cpf))
            {
                erros.Add(new ErroValidacao(nameof(vm.Cpf), "Já existe um profissional com este CPF."));
            }

            if (erros.Count > 0)
            {
                return RegistroResultado.Falha(erros);
            }

            // 1) Cria o usuário do Identity.
            var user = new IdentityUser
            {
                UserName = vm.Email,
                Email = vm.Email,
                EmailConfirmed = true
            };
            var createResult = await _userManager.CreateAsync(user, vm.Senha);
            if (!createResult.Succeeded)
            {
                return RegistroResultado.Falha(
                    createResult.Errors.Select(e => new ErroValidacao(string.Empty, e.Description)));
            }

            try
            {
                // 2) Role conforme o tipo (sem número mágico, falha explícita se não mapeado).
                var role = tipo!.IdTipoProfissional switch
                {
                    DbInitializer.TipoMedico => DbInitializer.RoleMedico,
                    DbInitializer.TipoNutricionista => DbInitializer.RoleNutricionista,
                    _ => throw new InvalidOperationException(
                        $"Tipo de profissional {tipo.IdTipoProfissional} não possui role mapeada.")
                };
                var roleResult = await _userManager.AddToRoleAsync(user, role);
                if (!roleResult.Succeeded)
                {
                    var msg = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Falha ao atribuir a role '{role}': {msg}");
                }

                // 3) Profissional + contrato (ligado ao plano) numa única gravação.
                var cidade = await _context.TbCidade.FirstOrDefaultAsync(c => c.IdCidade == vm.IdCidade);
                var profissional = new TbProfissional
                {
                    IdUser = user.Id,
                    Nome = vm.Nome,
                    Cpf = vm.Cpf,
                    CrmCrn = vm.CrmCrn,
                    Especialidade = vm.Especialidade,
                    Logradouro = vm.Logradouro,
                    Numero = vm.Numero,
                    Bairro = vm.Bairro,
                    Cep = vm.Cep,
                    Estado = vm.Estado,
                    Cidade = cidade?.Nome,
                    IdCidade = vm.IdCidade,
                    Ddd1 = vm.Ddd1,
                    Telefone1 = vm.Telefone1,
                    IdTipoProfissional = vm.IdTipoProfissional,
                    IdContratoNavigation = new TbContrato
                    {
                        IdPlano = vm.IdPlano,
                        DataInicio = DateTime.Now,
                        DataFim = DateTime.Now.AddDays(plano!.Validade)
                    }
                };

                _context.TbProfissional.Add(profissional);
                await _context.SaveChangesAsync();

                return RegistroResultado.Ok(user);
            }
            catch (Exception ex)
            {
                // Compensação: remove o usuário recém-criado para não deixar conta órfã.
                _logger.LogError(ex, "Falha ao criar o profissional de {Email}. Removendo o usuário órfão.", vm.Email);
                var del = await _userManager.DeleteAsync(user);
                if (!del.Succeeded)
                {
                    _logger.LogError("Não foi possível remover o usuário órfão {UserId} ({Email}): {Erros}",
                        user.Id, vm.Email, string.Join("; ", del.Errors.Select(e => e.Description)));
                }
                return RegistroResultado.Falha(string.Empty,
                    "Não foi possível concluir o cadastro. Tente novamente e, se persistir, contate o administrador.");
            }
        }

        // Convenção de nome: planos "Médico ..." são de médico (tipo 1) e "Nutri ..." de
        // nutricionista (tipo 2). Não há FK plano->tipo no design, então a ligação é por nome.
        private static int TipoDoPlano(string nome)
        {
            if (string.IsNullOrWhiteSpace(nome)) return 0;
            if (nome.StartsWith("Médico", StringComparison.OrdinalIgnoreCase)) return DbInitializer.TipoMedico;
            if (nome.StartsWith("Nutri", StringComparison.OrdinalIgnoreCase)) return DbInitializer.TipoNutricionista;
            return 0;
        }
    }
}
