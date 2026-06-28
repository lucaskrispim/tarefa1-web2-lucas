using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Projeto1_Web2_IF_Lucas.Models;
using Xunit;

namespace Projeto1_Web2_IF_Lucas.Tests
{
    // Bateria de testes de integração do Trabalho Final. Sobe o app real (Identity, migrations,
    // seed) sobre um SQLite temporário e exercita os fluxos e regras de segurança dos 3 requisitos.
    public class IntegrationTests : IClassFixture<TestAppFactory>
    {
        private readonly TestAppFactory _factory;

        public IntegrationTests(TestAppFactory factory) => _factory = factory;

        private HttpClient NovoCliente() =>
            _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        private static string Uniq() => Guid.NewGuid().ToString("N")[..10];

        private IServiceScope Escopo() => _factory.Services.CreateScope();

        // ---------------------------------------------------------------
        // Infra / disponibilidade
        // ---------------------------------------------------------------

        [Fact]
        public async Task Home_Retorna200()
        {
            var client = NovoCliente();
            var resp = await client.GetAsync("/");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        }

        [Fact]
        public async Task Seed_CriaCincoRolesTresGerentesETiposPlanos()
        {
            using var scope = Escopo();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var db = scope.ServiceProvider.GetRequiredService<db_ifContext>();

            foreach (var r in new[] { "Medico", "Nutricionista", "GerenteMedico", "GerenteNutricionista", "GerenteGeral" })
            {
                Assert.True(await roleManager.RoleExistsAsync(r), $"Role {r} deveria existir");
            }
            Assert.NotNull(await userManager.FindByEmailAsync("gerentegeral@saude.local"));
            Assert.NotNull(await userManager.FindByEmailAsync("gerentemedico@saude.local"));
            Assert.NotNull(await userManager.FindByEmailAsync("gerentenutri@saude.local"));
            Assert.True(await db.TbTipoProfissional.AnyAsync());
            Assert.True(await db.TbPlano.AnyAsync());
        }

        // ---------------------------------------------------------------
        // Autenticação / acesso anônimo
        // ---------------------------------------------------------------

        [Fact]
        public async Task Anonimo_MeusPacientes_RedirecionaParaLogin()
        {
            var client = NovoCliente();
            var resp = await client.GetAsync("/MeusPacientes");
            Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
            Assert.Contains("/Identity/Account/Login", resp.Headers.Location!.ToString());
        }

        [Fact]
        public async Task RegistroPadraoIdentity_RedirecionaParaConta()
        {
            var client = NovoCliente();
            var resp = await client.GetAsync("/Identity/Account/Register");
            Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
            Assert.Contains("/Conta/Registrar", resp.Headers.Location!.ToString());
        }

        // ---------------------------------------------------------------
        // Requisito 1 - Registro do profissional
        // ---------------------------------------------------------------

        [Fact]
        public async Task Registro_Medico_CriaUsuarioRoleProfissionalEContrato()
        {
            var client = NovoCliente();
            var email = $"med_{Uniq()}@s.local";
            var cpf = $"CPF-{Uniq()}";

            var resp = await client.RegistrarProfissionalAsync(1, email, "Dra Teste", cpf, idPlano: 1);
            Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);

            using var scope = Escopo();
            var db = scope.ServiceProvider.GetRequiredService<db_ifContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            var prof = await db.TbProfissional.FirstOrDefaultAsync(p => p.Cpf == cpf);
            Assert.NotNull(prof);
            Assert.Equal(1, prof!.IdTipoProfissional);

            var contrato = await db.TbContrato.FirstOrDefaultAsync(c => c.IdContrato == prof.IdContrato);
            Assert.NotNull(contrato);

            var user = await userManager.FindByEmailAsync(email);
            Assert.NotNull(user);
            Assert.Equal(user!.Id, prof.IdUser);
            Assert.True(await userManager.IsInRoleAsync(user, "Medico"));
            Assert.False(await userManager.IsInRoleAsync(user, "Nutricionista"));
        }

        [Fact]
        public async Task Registro_PlanoIncompativelComTipo_EhRejeitadoSemCriarUsuario()
        {
            var client = NovoCliente();
            var email = $"bad_{Uniq()}@s.local";

            // Médico (tipo 1) escolhendo um plano de Nutricionista (Nutri Total = id 3).
            var resp = await client.RegistrarProfissionalAsync(1, email, "Invalido", $"CPF-{Uniq()}", idPlano: 3);
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode); // re-render com erro

            using var scope = Escopo();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            Assert.Null(await userManager.FindByEmailAsync(email)); // não criou usuário órfão
        }

        // ---------------------------------------------------------------
        // Requisito 3 - CRUD de pacientes e ownership
        // ---------------------------------------------------------------

        [Fact]
        public async Task Profissional_CriaEVeoSeuPaciente()
        {
            var client = NovoCliente();
            await client.RegistrarProfissionalAsync(1, $"p_{Uniq()}@s.local", "Prof", $"CPF-{Uniq()}", 1);

            var nomePaciente = $"Paciente {Uniq()}";
            var resp = await client.CriarPacienteAsync(nomePaciente, $"PAC-{Uniq()}");
            var corpo = resp.StatusCode == HttpStatusCode.Redirect ? "" : await resp.Content.ReadAsStringAsync();
            Assert.True(resp.StatusCode == HttpStatusCode.Redirect, $"Status={resp.StatusCode}. Corpo:\n{corpo}");

            var index = await client.GetStringAsync("/MeusPacientes");
            Assert.Contains(nomePaciente, index);
        }

        [Fact]
        public async Task Profissional_NaoAcessaPacienteDeOutro()
        {
            // Profissional A cria um paciente.
            var clienteA = NovoCliente();
            await clienteA.RegistrarProfissionalAsync(1, $"a_{Uniq()}@s.local", "ProfA", $"CPF-{Uniq()}", 1);
            var nomePaciente = $"PacienteA {Uniq()}";
            await clienteA.CriarPacienteAsync(nomePaciente, $"PAC-{Uniq()}");

            int idPaciente;
            using (var scope = Escopo())
            {
                var db = scope.ServiceProvider.GetRequiredService<db_ifContext>();
                idPaciente = await db.TbPaciente.Where(p => p.Nome == nomePaciente)
                    .Select(p => p.IdPaciente).FirstAsync();
            }

            // Profissional B tenta acessar o paciente de A.
            var clienteB = NovoCliente();
            await clienteB.RegistrarProfissionalAsync(1, $"b_{Uniq()}@s.local", "ProfB", $"CPF-{Uniq()}", 1);

            var respDetalhes = await clienteB.GetAsync($"/MeusPacientes/Detalhes/{idPaciente}");
            Assert.Equal(HttpStatusCode.NotFound, respDetalhes.StatusCode);

            var respEditar = await clienteB.GetAsync($"/MeusPacientes/Editar/{idPaciente}");
            Assert.Equal(HttpStatusCode.NotFound, respEditar.StatusCode);

            // E A continua acessando o seu.
            var respA = await clienteA.GetAsync($"/MeusPacientes/Detalhes/{idPaciente}");
            Assert.Equal(HttpStatusCode.OK, respA.StatusCode);
        }

        // ---------------------------------------------------------------
        // Autorização por papel (Controller) - Requisitos 1 e 3
        // ---------------------------------------------------------------

        [Fact]
        public async Task Profissional_BloqueadoNaAreaDeGerentes()
        {
            var client = NovoCliente();
            await client.RegistrarProfissionalAsync(1, $"pg_{Uniq()}@s.local", "Prof", $"CPF-{Uniq()}", 1);

            var resp = await client.GetAsync("/Profissionais");
            Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
            Assert.Contains("AccessDenied", resp.Headers.Location!.ToString());
        }

        [Fact]
        public async Task Profissional_BloqueadoNosControllersLegados()
        {
            var client = NovoCliente();
            await client.RegistrarProfissionalAsync(1, $"pl_{Uniq()}@s.local", "Prof", $"CPF-{Uniq()}", 1);

            foreach (var url in new[] { "/TbPacientes", "/Pacientes" })
            {
                var resp = await client.GetAsync(url);
                Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
                Assert.Contains("AccessDenied", resp.Headers.Location!.ToString());
            }
        }

        // ---------------------------------------------------------------
        // Requisito 2 - Escopo dos gerentes
        // ---------------------------------------------------------------

        [Fact]
        public async Task Registro_Nutricionista_RecebeRoleNutricionista()
        {
            var client = NovoCliente();
            var email = $"nut_{Uniq()}@s.local";
            var cpf = $"CPF-{Uniq()}";
            // tipo 2 (Nutricionista) + plano de nutri (Nutri Total = id 3).
            var resp = await client.RegistrarProfissionalAsync(2, email, "Nutri Reg", cpf, idPlano: 3);
            Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);

            using var scope = Escopo();
            var db = scope.ServiceProvider.GetRequiredService<db_ifContext>();
            var um = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var prof = await db.TbProfissional.FirstAsync(p => p.Cpf == cpf);
            Assert.Equal(2, prof.IdTipoProfissional);
            var user = await um.FindByEmailAsync(email);
            Assert.True(await um.IsInRoleAsync(user!, "Nutricionista"));
            Assert.False(await um.IsInRoleAsync(user!, "Medico"));
        }

        [Fact]
        public async Task Gerente_NaoPodeCriarProfissional()
        {
            var gerente = NovoCliente();
            await gerente.LoginAsync("gerentegeral@saude.local", "Gerente@123");

            // Não existe action de criação no controller de gerentes -> 404.
            foreach (var url in new[] { "/Profissionais/Criar", "/Profissionais/Create" })
            {
                var resp = await gerente.GetAsync(url);
                Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
            }
        }

        [Fact]
        public async Task GerenteNutricionista_VeNutricionistasMasNaoMedicos()
        {
            var nomeMedico = $"Med {Uniq()}";
            var nomeNutri = $"Nutri {Uniq()}";
            var cMed = NovoCliente();
            await cMed.RegistrarProfissionalAsync(1, $"m_{Uniq()}@s.local", nomeMedico, $"CPF-{Uniq()}", 1);
            var cNut = NovoCliente();
            await cNut.RegistrarProfissionalAsync(2, $"n_{Uniq()}@s.local", nomeNutri, $"CPF-{Uniq()}", 3);

            var gerente = NovoCliente();
            await gerente.LoginAsync("gerentenutri@saude.local", "Gerente@123");

            var index = await gerente.GetStringAsync("/Profissionais");
            Assert.Contains(nomeNutri, index);
            Assert.DoesNotContain(nomeMedico, index);
        }

        [Fact]
        public async Task GerenteGeral_VeMedicosENutricionistas()
        {
            var nomeMedico = $"Med {Uniq()}";
            var nomeNutri = $"Nutri {Uniq()}";
            var cMed = NovoCliente();
            await cMed.RegistrarProfissionalAsync(1, $"m_{Uniq()}@s.local", nomeMedico, $"CPF-{Uniq()}", 1);
            var cNut = NovoCliente();
            await cNut.RegistrarProfissionalAsync(2, $"n_{Uniq()}@s.local", nomeNutri, $"CPF-{Uniq()}", 3);

            var gerente = NovoCliente();
            await gerente.LoginAsync("gerentegeral@saude.local", "Gerente@123");

            var index = await gerente.GetStringAsync("/Profissionais");
            Assert.Contains(nomeMedico, index);
            Assert.Contains(nomeNutri, index);
        }

        [Fact]
        public async Task GerenteMedico_VeMedicosMasNaoNutricionistas()
        {
            // Cria um médico e um nutricionista.
            var nomeMedico = $"Med {Uniq()}";
            var nomeNutri = $"Nutri {Uniq()}";
            var cMed = NovoCliente();
            await cMed.RegistrarProfissionalAsync(1, $"m_{Uniq()}@s.local", nomeMedico, $"CPF-{Uniq()}", 1);
            var cNut = NovoCliente();
            await cNut.RegistrarProfissionalAsync(2, $"n_{Uniq()}@s.local", nomeNutri, $"CPF-{Uniq()}", 3);

            var gerente = NovoCliente();
            var login = await gerente.LoginAsync("gerentemedico@saude.local", "Gerente@123");
            Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);

            var index = await gerente.GetStringAsync("/Profissionais");
            Assert.Contains(nomeMedico, index);
            Assert.DoesNotContain(nomeNutri, index);
        }

        [Fact]
        public async Task GerenteMedico_ForaDoEscopo_RecebeAccessDenied()
        {
            // Cria um nutricionista e descobre o seu IdProfissional.
            var cNut = NovoCliente();
            var cpf = $"CPF-{Uniq()}";
            await cNut.RegistrarProfissionalAsync(2, $"n_{Uniq()}@s.local", "Nutri X", cpf, 3);

            int idProf;
            using (var scope = Escopo())
            {
                var db = scope.ServiceProvider.GetRequiredService<db_ifContext>();
                idProf = await db.TbProfissional.Where(p => p.Cpf == cpf)
                    .Select(p => p.IdProfissional).FirstAsync();
            }

            var gerente = NovoCliente();
            await gerente.LoginAsync("gerentemedico@saude.local", "Gerente@123");

            var resp = await gerente.GetAsync($"/Profissionais/Detalhes/{idProf}");
            Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
            Assert.Contains("AccessDenied", resp.Headers.Location!.ToString());
        }

        [Fact]
        public async Task Profissional_EditaSeuPaciente_Persiste()
        {
            var client = NovoCliente();
            await client.RegistrarProfissionalAsync(1, $"e_{Uniq()}@s.local", "Prof", $"CPF-{Uniq()}", 1);
            var cpfPac = $"PAC-{Uniq()}";
            await client.CriarPacienteAsync($"Pac {Uniq()}", cpfPac);

            int idPac;
            using (var scope = Escopo())
            {
                var db = scope.ServiceProvider.GetRequiredService<db_ifContext>();
                idPac = await db.TbPaciente.Where(p => p.Cpf == cpfPac).Select(p => p.IdPaciente).FirstAsync();
            }

            var novoNome = $"Editado {Uniq()}";
            var resp = await client.PostComTokenAsync(
                $"/MeusPacientes/Editar/{idPac}", $"/MeusPacientes/Editar/{idPac}",
                new Dictionary<string, string>
                {
                    ["Nome"] = novoNome, ["Rg"] = "RG1", ["Cpf"] = cpfPac,
                    ["DataNascimento"] = "1990-05-20", ["Sexo"] = "F", ["Etnia"] = "2", ["IdCidade"] = "2"
                });
            Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);

            using (var scope = Escopo())
            {
                var db = scope.ServiceProvider.GetRequiredService<db_ifContext>();
                var pac = await db.TbPaciente.FirstAsync(p => p.IdPaciente == idPac);
                Assert.Equal(novoNome, pac.Nome);
            }
        }

        [Fact]
        public async Task Profissional_NaoEditaNemExcluiPacienteDeOutro_ViaPost()
        {
            var clienteA = NovoCliente();
            await clienteA.RegistrarProfissionalAsync(1, $"a2_{Uniq()}@s.local", "ProfA", $"CPF-{Uniq()}", 1);
            var cpfPac = $"PAC-{Uniq()}";
            var nomeOriginal = $"Original {Uniq()}";
            await clienteA.CriarPacienteAsync(nomeOriginal, cpfPac);

            int idPac;
            using (var scope = Escopo())
            {
                var db = scope.ServiceProvider.GetRequiredService<db_ifContext>();
                idPac = await db.TbPaciente.Where(p => p.Cpf == cpfPac).Select(p => p.IdPaciente).FirstAsync();
            }

            var clienteB = NovoCliente();
            await clienteB.RegistrarProfissionalAsync(1, $"b2_{Uniq()}@s.local", "ProfB", $"CPF-{Uniq()}", 1);

            // B tenta EDITAR o paciente de A via POST -> NotFound, dados intactos.
            var respEdit = await clienteB.PostComTokenAsync(
                "/MeusPacientes/Criar", $"/MeusPacientes/Editar/{idPac}",
                new Dictionary<string, string>
                {
                    ["Nome"] = "Hackeado", ["Rg"] = "RG1", ["Cpf"] = cpfPac,
                    ["DataNascimento"] = "1990-05-20", ["Sexo"] = "M", ["Etnia"] = "1", ["IdCidade"] = "1"
                });
            Assert.Equal(HttpStatusCode.NotFound, respEdit.StatusCode);

            // B tenta EXCLUIR o paciente de A via POST -> não remove.
            await clienteB.PostComTokenAsync(
                "/MeusPacientes/Criar", $"/MeusPacientes/Excluir/{idPac}", new Dictionary<string, string>());

            using (var scope = Escopo())
            {
                var db = scope.ServiceProvider.GetRequiredService<db_ifContext>();
                var pac = await db.TbPaciente.FirstOrDefaultAsync(p => p.IdPaciente == idPac);
                Assert.NotNull(pac);
                Assert.Equal(nomeOriginal, pac!.Nome); // não foi alterado por B
            }
        }

        [Fact]
        public async Task Profissional_ExcluiPaciente_RemoveOrfaoMasMantemSeAindaVinculado()
        {
            var client = NovoCliente();
            var emailA = $"d_{Uniq()}@s.local";
            await client.RegistrarProfissionalAsync(1, emailA, "ProfDel", $"CPF-{Uniq()}", 1);
            var cpfPac = $"PAC-{Uniq()}";
            await client.CriarPacienteAsync($"PacDel {Uniq()}", cpfPac);

            int idPac, idProfA;
            using (var scope = Escopo())
            {
                var db = scope.ServiceProvider.GetRequiredService<db_ifContext>();
                var um = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                idPac = await db.TbPaciente.Where(p => p.Cpf == cpfPac).Select(p => p.IdPaciente).FirstAsync();
                var userA = await um.FindByEmailAsync(emailA);
                idProfA = await db.TbProfissional.Where(p => p.IdUser == userA!.Id).Select(p => p.IdProfissional).FirstAsync();
            }

            // Simula um segundo profissional vinculado ao mesmo paciente (paciente N:N).
            using (var scope = Escopo())
            {
                var db = scope.ServiceProvider.GetRequiredService<db_ifContext>();
                var outroProf = await db.TbProfissional.Where(p => p.IdProfissional != idProfA)
                    .Select(p => p.IdProfissional).FirstOrDefaultAsync();
                if (outroProf != 0)
                {
                    db.TbMedicoPaciente.Add(new TbMedicoPaciente
                    {
                        IdPaciente = idPac, IdProfissional = outroProf, InformacaoResumida = "outro"
                    });
                    await db.SaveChangesAsync();
                }
            }

            // A exclui: o vínculo de A some, mas o paciente permanece (ainda vinculado ao outro).
            await client.PostComTokenAsync(
                $"/MeusPacientes/Excluir/{idPac}", $"/MeusPacientes/Excluir/{idPac}", new Dictionary<string, string>());

            using (var scope = Escopo())
            {
                var db = scope.ServiceProvider.GetRequiredService<db_ifContext>();
                Assert.False(await db.TbMedicoPaciente.AnyAsync(mp => mp.IdPaciente == idPac && mp.IdProfissional == idProfA));
                Assert.True(await db.TbPaciente.AnyAsync(p => p.IdPaciente == idPac)); // mantido (ainda vinculado)
            }
        }

        [Fact]
        public async Task MeuPerfil_NaoAlteraCpf_PorOverposting()
        {
            var client = NovoCliente();
            var email = $"perf_{Uniq()}@s.local";
            var cpf = $"CPF-{Uniq()}";
            await client.RegistrarProfissionalAsync(1, email, "Perfil", cpf, 1);

            var resp = await client.PostComTokenAsync(
                "/MeuPerfil/Editar", "/MeuPerfil/Editar",
                new Dictionary<string, string>
                {
                    ["Nome"] = "Nome Novo", ["Cpf"] = "CPF-HACKEADO",
                    ["Numero"] = "1", ["Bairro"] = "Centro", ["Cep"] = "69000-000", ["IdCidade"] = "1"
                });
            Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);

            using var scope = Escopo();
            var db = scope.ServiceProvider.GetRequiredService<db_ifContext>();
            var um = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var user = await um.FindByEmailAsync(email);
            var prof = await db.TbProfissional.FirstAsync(p => p.IdUser == user!.Id);
            Assert.Equal(cpf, prof.Cpf);          // CPF preservado (não alterável pelo próprio profissional)
            Assert.Equal("Nome Novo", prof.Nome); // Nome atualizado
        }

        [Fact]
        public async Task Gerente_AlteraCpfDoProfissional_Persiste()
        {
            var cMed = NovoCliente();
            var cpf = $"CPF-{Uniq()}";
            await cMed.RegistrarProfissionalAsync(1, $"gm_{Uniq()}@s.local", "MedCpf", cpf, 1);

            int idProf;
            using (var scope = Escopo())
            {
                var db = scope.ServiceProvider.GetRequiredService<db_ifContext>();
                idProf = await db.TbProfissional.Where(p => p.Cpf == cpf).Select(p => p.IdProfissional).FirstAsync();
            }

            var gerente = NovoCliente();
            await gerente.LoginAsync("gerentegeral@saude.local", "Gerente@123");

            var novoCpf = $"NOVO-{Uniq()}";
            var resp = await gerente.PostComTokenAsync(
                $"/Profissionais/Editar/{idProf}", $"/Profissionais/Editar/{idProf}",
                new Dictionary<string, string>
                {
                    ["Nome"] = "MedCpf", ["Cpf"] = novoCpf,
                    ["Numero"] = "1", ["Bairro"] = "Centro", ["Cep"] = "69000-000", ["IdCidade"] = "1"
                });
            Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);

            using (var scope = Escopo())
            {
                var db = scope.ServiceProvider.GetRequiredService<db_ifContext>();
                var prof = await db.TbProfissional.FirstAsync(p => p.IdProfissional == idProf);
                Assert.Equal(novoCpf, prof.Cpf); // gerente PODE alterar o CPF
            }
        }

        [Fact]
        public async Task Gerente_ExcluiProfissionalSemPacientes_RemoveProfContratoELogin()
        {
            var cMed = NovoCliente();
            var email = $"sempac_{Uniq()}@s.local";
            var cpf = $"CPF-{Uniq()}";
            await cMed.RegistrarProfissionalAsync(1, email, "Sem Pacientes", cpf, 1);

            int idProf, idContrato; string idUser;
            using (var scope = Escopo())
            {
                var db = scope.ServiceProvider.GetRequiredService<db_ifContext>();
                var um = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                var prof = await db.TbProfissional.FirstAsync(p => p.Cpf == cpf);
                idProf = prof.IdProfissional; idContrato = prof.IdContrato; idUser = prof.IdUser;
            }

            var gerente = NovoCliente();
            await gerente.LoginAsync("gerentegeral@saude.local", "Gerente@123");
            var resp = await gerente.PostComTokenAsync(
                $"/Profissionais/Excluir/{idProf}", $"/Profissionais/Excluir/{idProf}", new Dictionary<string, string>());
            Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);

            using (var scope = Escopo())
            {
                var db = scope.ServiceProvider.GetRequiredService<db_ifContext>();
                var um = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                Assert.False(await db.TbProfissional.AnyAsync(p => p.IdProfissional == idProf));
                Assert.False(await db.TbContrato.AnyAsync(c => c.IdContrato == idContrato));
                Assert.Null(await um.FindByIdAsync(idUser));
            }
        }

        [Fact]
        public async Task Registro_CpfDuplicado_RejeitaSemUsuarioOrfao()
        {
            var cpf = $"CPF-{Uniq()}";
            var c1 = NovoCliente();
            var r1 = await c1.RegistrarProfissionalAsync(1, $"dup1_{Uniq()}@s.local", "Primeiro", cpf, 1);
            Assert.Equal(HttpStatusCode.Redirect, r1.StatusCode);

            var c2 = NovoCliente();
            var email2 = $"dup2_{Uniq()}@s.local";
            var r2 = await c2.RegistrarProfissionalAsync(1, email2, "Segundo", cpf, 1);
            Assert.Equal(HttpStatusCode.OK, r2.StatusCode); // re-render com erro de CPF

            using var scope = Escopo();
            var db = scope.ServiceProvider.GetRequiredService<db_ifContext>();
            var um = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            Assert.Equal(1, await db.TbProfissional.CountAsync(p => p.Cpf == cpf));
            Assert.Null(await um.FindByEmailAsync(email2)); // não criou usuário órfão
        }

        [Fact]
        public async Task Gerente_NaoExcluiProfissionalComPacientes()
        {
            // Médico com um paciente cadastrado.
            var cMed = NovoCliente();
            var cpf = $"CPF-{Uniq()}";
            await cMed.RegistrarProfissionalAsync(1, $"md_{Uniq()}@s.local", "Med Del", cpf, 1);
            await cMed.CriarPacienteAsync($"Pac {Uniq()}", $"PAC-{Uniq()}");

            int idProf;
            using (var scope = Escopo())
            {
                var db = scope.ServiceProvider.GetRequiredService<db_ifContext>();
                idProf = await db.TbProfissional.Where(p => p.Cpf == cpf)
                    .Select(p => p.IdProfissional).FirstAsync();
            }

            var gerente = NovoCliente();
            await gerente.LoginAsync("gerentegeral@saude.local", "Gerente@123");

            // POST de exclusão com antiforgery vindo da página de confirmação.
            var token = await gerente.ObterTokenAsync($"/Profissionais/Excluir/{idProf}");
            var resp = await gerente.PostAsync($"/Profissionais/Excluir/{idProf}",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["__RequestVerificationToken"] = token
                }));

            Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);

            // O profissional NÃO pode ter sido excluído (tem paciente).
            using (var scope = Escopo())
            {
                var db = scope.ServiceProvider.GetRequiredService<db_ifContext>();
                Assert.True(await db.TbProfissional.AnyAsync(p => p.IdProfissional == idProf));
            }
        }

        // ---------------------------------------------------------------
        // Extra - exclusão em cascata
        // ---------------------------------------------------------------

        [Fact]
        public async Task GerenteCascata_ExcluiProfissionalEPacientesExclusivos()
        {
            var cMed = NovoCliente();
            var cpf = $"CPF-{Uniq()}";
            await cMed.RegistrarProfissionalAsync(1, $"cas_{Uniq()}@s.local", "Med Cascata", cpf, 1);
            var cpfPac1 = $"PAC-{Uniq()}";
            var cpfPac2 = $"PAC-{Uniq()}";
            await cMed.CriarPacienteAsync($"Pac1 {Uniq()}", cpfPac1);
            await cMed.CriarPacienteAsync($"Pac2 {Uniq()}", cpfPac2);

            int idProf, idContrato; string idUser;
            using (var scope = Escopo())
            {
                var db = scope.ServiceProvider.GetRequiredService<db_ifContext>();
                var prof = await db.TbProfissional.FirstAsync(p => p.Cpf == cpf);
                idProf = prof.IdProfissional; idContrato = prof.IdContrato; idUser = prof.IdUser;
            }

            var gerente = NovoCliente();
            await gerente.LoginAsync("gerentegeral@saude.local", "Gerente@123");
            var resp = await gerente.PostComTokenAsync(
                $"/Profissionais/Excluir/{idProf}", $"/Profissionais/Excluir/{idProf}",
                new Dictionary<string, string> { ["cascata"] = "true" });
            Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);

            using (var scope = Escopo())
            {
                var db = scope.ServiceProvider.GetRequiredService<db_ifContext>();
                var um = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                Assert.False(await db.TbProfissional.AnyAsync(p => p.IdProfissional == idProf));
                Assert.False(await db.TbContrato.AnyAsync(c => c.IdContrato == idContrato));
                Assert.Null(await um.FindByIdAsync(idUser));
                // Pacientes exclusivos foram removidos junto.
                Assert.False(await db.TbPaciente.AnyAsync(p => p.Cpf == cpfPac1));
                Assert.False(await db.TbPaciente.AnyAsync(p => p.Cpf == cpfPac2));
            }
        }

        [Fact]
        public async Task GerenteCascata_PreservaPacienteCompartilhado()
        {
            // Profissional A com um paciente.
            var cA = NovoCliente();
            var cpfA = $"CPF-{Uniq()}";
            await cA.RegistrarProfissionalAsync(1, $"ca_{Uniq()}@s.local", "Med A", cpfA, 1);
            var cpfPac = $"PAC-{Uniq()}";
            await cA.CriarPacienteAsync($"Compart {Uniq()}", cpfPac);

            // Profissional B (para compartilhar o paciente).
            var cB = NovoCliente();
            var cpfB = $"CPF-{Uniq()}";
            await cB.RegistrarProfissionalAsync(1, $"cb_{Uniq()}@s.local", "Med B", cpfB, 1);

            int idProfA, idProfB, idPac;
            using (var scope = Escopo())
            {
                var db = scope.ServiceProvider.GetRequiredService<db_ifContext>();
                idProfA = await db.TbProfissional.Where(p => p.Cpf == cpfA).Select(p => p.IdProfissional).FirstAsync();
                idProfB = await db.TbProfissional.Where(p => p.Cpf == cpfB).Select(p => p.IdProfissional).FirstAsync();
                idPac = await db.TbPaciente.Where(p => p.Cpf == cpfPac).Select(p => p.IdPaciente).FirstAsync();
                // Compartilha o paciente de A com B.
                db.TbMedicoPaciente.Add(new TbMedicoPaciente
                {
                    IdPaciente = idPac, IdProfissional = idProfB, InformacaoResumida = "compartilhado"
                });
                await db.SaveChangesAsync();
            }

            var gerente = NovoCliente();
            await gerente.LoginAsync("gerentegeral@saude.local", "Gerente@123");
            await gerente.PostComTokenAsync(
                $"/Profissionais/Excluir/{idProfA}", $"/Profissionais/Excluir/{idProfA}",
                new Dictionary<string, string> { ["cascata"] = "true" });

            using (var scope = Escopo())
            {
                var db = scope.ServiceProvider.GetRequiredService<db_ifContext>();
                Assert.False(await db.TbProfissional.AnyAsync(p => p.IdProfissional == idProfA)); // A removido
                Assert.True(await db.TbPaciente.AnyAsync(p => p.IdPaciente == idPac));            // paciente preservado (compartilhado)
                Assert.False(await db.TbMedicoPaciente.AnyAsync(mp => mp.IdProfissional == idProfA)); // vínculo de A removido
                Assert.True(await db.TbMedicoPaciente.AnyAsync(mp => mp.IdProfissional == idProfB && mp.IdPaciente == idPac)); // vínculo de B mantido
            }
        }

        [Fact]
        public async Task GerenteCascata_PreservaPacienteCompartilhadoEntreTiposDiferentes()
        {
            // Médico (tipo 1) com um paciente; o paciente é compartilhado com um Nutricionista (tipo 2).
            var cMed = NovoCliente();
            var cpfMed = $"CPF-{Uniq()}";
            await cMed.RegistrarProfissionalAsync(1, $"mt_{Uniq()}@s.local", "Med Tipo1", cpfMed, 1);
            var cpfPac = $"PAC-{Uniq()}";
            await cMed.CriarPacienteAsync($"PacInter {Uniq()}", cpfPac);

            var cNut = NovoCliente();
            var cpfNut = $"CPF-{Uniq()}";
            await cNut.RegistrarProfissionalAsync(2, $"nt_{Uniq()}@s.local", "Nutri Tipo2", cpfNut, 3);

            int idMed, idNut, idPac;
            using (var scope = Escopo())
            {
                var db = scope.ServiceProvider.GetRequiredService<db_ifContext>();
                idMed = await db.TbProfissional.Where(p => p.Cpf == cpfMed).Select(p => p.IdProfissional).FirstAsync();
                idNut = await db.TbProfissional.Where(p => p.Cpf == cpfNut).Select(p => p.IdProfissional).FirstAsync();
                idPac = await db.TbPaciente.Where(p => p.Cpf == cpfPac).Select(p => p.IdPaciente).FirstAsync();
                db.TbMedicoPaciente.Add(new TbMedicoPaciente
                {
                    IdPaciente = idPac, IdProfissional = idNut, InformacaoResumida = "compartilhado entre tipos"
                });
                await db.SaveChangesAsync();
            }

            // GerenteMedico (escopo médico) exclui o médico em cascata.
            var gerente = NovoCliente();
            await gerente.LoginAsync("gerentemedico@saude.local", "Gerente@123");
            await gerente.PostComTokenAsync(
                $"/Profissionais/Excluir/{idMed}", $"/Profissionais/Excluir/{idMed}",
                new Dictionary<string, string> { ["cascata"] = "true" });

            using (var scope = Escopo())
            {
                var db = scope.ServiceProvider.GetRequiredService<db_ifContext>();
                Assert.False(await db.TbProfissional.AnyAsync(p => p.IdProfissional == idMed));  // médico removido
                Assert.True(await db.TbProfissional.AnyAsync(p => p.IdProfissional == idNut));   // nutricionista intacto
                Assert.True(await db.TbPaciente.AnyAsync(p => p.IdPaciente == idPac));           // paciente preservado (a regra é por vínculo, não por tipo)
                Assert.True(await db.TbMedicoPaciente.AnyAsync(mp => mp.IdProfissional == idNut && mp.IdPaciente == idPac));
            }
        }

        [Fact]
        public async Task GerenteCascata_ProfissionalSemPacientes_ExcluiNormalmente()
        {
            var cMed = NovoCliente();
            var cpf = $"CPF-{Uniq()}";
            await cMed.RegistrarProfissionalAsync(1, $"sp_{Uniq()}@s.local", "Sem Pac", cpf, 1);

            int idProf;
            using (var scope = Escopo())
            {
                var db = scope.ServiceProvider.GetRequiredService<db_ifContext>();
                idProf = await db.TbProfissional.Where(p => p.Cpf == cpf).Select(p => p.IdProfissional).FirstAsync();
            }

            var gerente = NovoCliente();
            await gerente.LoginAsync("gerentegeral@saude.local", "Gerente@123");
            var resp = await gerente.PostComTokenAsync(
                $"/Profissionais/Excluir/{idProf}", $"/Profissionais/Excluir/{idProf}",
                new Dictionary<string, string> { ["cascata"] = "true" });
            Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);

            using (var scope = Escopo())
            {
                var db = scope.ServiceProvider.GetRequiredService<db_ifContext>();
                Assert.False(await db.TbProfissional.AnyAsync(p => p.IdProfissional == idProf));
            }
        }
    }
}
