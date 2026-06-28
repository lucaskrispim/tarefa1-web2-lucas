using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Projeto1_Web2_IF_Lucas.Models;

// Lucas Wilman da Silva Crispim
namespace Projeto1_Web2_IF_Lucas.Data
{
    // Inicialização (seed) idempotente executada no startup:
    //  - cria as 5 Roles (Médico, Nutricionista e os 3 gerentes);
    //  - cria os 3 usuários Gerentes e associa as Roles direto no banco;
    //  - popula dados de apoio (tipos de profissional, cidades e planos).
    // Tudo é verificado antes de inserir, então rodar várias vezes não duplica nada.
    public static class DbInitializer
    {
        // Nomes das roles centralizados para reuso nos controllers ([Authorize(Roles = ...)]).
        public const string RoleMedico = "Medico";
        public const string RoleNutricionista = "Nutricionista";
        public const string RoleGerenteMedico = "GerenteMedico";
        public const string RoleGerenteNutricionista = "GerenteNutricionista";
        public const string RoleGerenteGeral = "GerenteGeral";

        // Ids estáveis dos tipos de profissional (seedados nesta ordem). Centralizados para
        // evitar "número mágico" no registro e no filtro dos gerentes por tipo.
        public const int TipoMedico = 1;
        public const int TipoNutricionista = 2;

        public static async Task SeedAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
            var db = services.GetRequiredService<db_ifContext>();

            await SeedRolesAsync(roleManager);
            await SeedGerentesAsync(userManager);
            await SeedDadosApoioAsync(db);
        }

        // 1) Roles
        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            string[] roles =
            {
                RoleMedico, RoleNutricionista,
                RoleGerenteMedico, RoleGerenteNutricionista, RoleGerenteGeral
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var result = await roleManager.CreateAsync(new IdentityRole(role));
                    if (!result.Succeeded)
                    {
                        var erros = string.Join("; ", result.Errors.Select(e => e.Description));
                        throw new InvalidOperationException($"Falha ao criar a role '{role}': {erros}");
                    }
                }
            }
        }

        // 2) Três gerentes especiais, com as roles associadas direto no banco.
        private static async Task SeedGerentesAsync(UserManager<IdentityUser> userManager)
        {
            await EnsureUserInRoleAsync(userManager, "gerentemedico@saude.local", "Gerente@123", RoleGerenteMedico);
            await EnsureUserInRoleAsync(userManager, "gerentenutri@saude.local", "Gerente@123", RoleGerenteNutricionista);
            await EnsureUserInRoleAsync(userManager, "gerentegeral@saude.local", "Gerente@123", RoleGerenteGeral);
        }

        private static async Task EnsureUserInRoleAsync(
            UserManager<IdentityUser> userManager, string email, string senha, string role)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new IdentityUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(user, senha);
                if (!result.Succeeded)
                {
                    var erros = string.Join("; ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Falha ao criar o usuário '{email}': {erros}");
                }
            }

            if (!await userManager.IsInRoleAsync(user, role))
            {
                var addResult = await userManager.AddToRoleAsync(user, role);
                if (!addResult.Succeeded)
                {
                    var erros = string.Join("; ", addResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Falha ao associar '{email}' à role '{role}': {erros}");
                }
            }
        }

        // 3) Dados de apoio do db_if (tipos, cidades e planos).
        private static async Task SeedDadosApoioAsync(db_ifContext db)
        {
            // Tipos de profissional (ordem garante Médico antes de Nutricionista).
            if (!await db.TbTipoProfissional.AnyAsync())
            {
                db.TbTipoProfissional.AddRange(
                    new TbTipoProfissional { Nome = "Médico" },
                    new TbTipoProfissional { Nome = "Nutricionista" });
                await db.SaveChangesAsync();
            }

            // Cidades para seleção no cadastro do profissional.
            if (!await db.TbCidade.AnyAsync())
            {
                db.TbCidade.AddRange(
                    new TbCidade { Nome = "Manaus" },
                    new TbCidade { Nome = "São Paulo" },
                    new TbCidade { Nome = "Rio de Janeiro" },
                    new TbCidade { Nome = "Belo Horizonte" });
                await db.SaveChangesAsync();
            }

            // Planos: por convenção de nome, "Médico ..." para médicos e "Nutri ..." para nutricionistas.
            if (!await db.TbPlano.AnyAsync())
            {
                db.TbPlano.AddRange(
                    new TbPlano { Nome = "Médico Total", Validade = 365, Valor = 1200.00m },
                    new TbPlano { Nome = "Médico Parcial", Validade = 365, Valor = 700.00m },
                    new TbPlano { Nome = "Nutri Total", Validade = 365, Valor = 900.00m },
                    new TbPlano { Nome = "Nutri Parcial", Validade = 365, Valor = 500.00m });
                await db.SaveChangesAsync();
            }
        }
    }
}
