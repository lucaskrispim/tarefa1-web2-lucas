using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Projeto1_Web2_IF_Lucas.Data;
using Projeto1_Web2_IF_Lucas.Models;
using Projeto1_Web2_IF_Lucas.Models.ViewModels;
using Projeto1_Web2_IF_Lucas.Services;
using Xunit;

namespace Projeto1_Web2_IF_Lucas.Tests.Unit
{
    // Testes UNITÁRIOS do RegistroService (db_ifContext in-memory + UserManager mockado).
    public class RegistroServiceTests : IDisposable
    {
        private readonly db_ifContext _ctx;
        private readonly SqliteConnection _conn;
        private readonly Mock<UserManager<IdentityUser>> _userMgr;
        private readonly RegistroService _svc;

        public RegistroServiceTests()
        {
            (_ctx, _conn) = TestDb.Criar();
            _userMgr = TestDb.MockUserManager();
            _userMgr.Setup(m => m.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            _userMgr.Setup(m => m.AddToRoleAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            _userMgr.Setup(m => m.DeleteAsync(It.IsAny<IdentityUser>()))
                .ReturnsAsync(IdentityResult.Success);
            _svc = new RegistroService(_ctx, _userMgr.Object, NullLogger<RegistroService>.Instance);
        }

        public void Dispose()
        {
            _ctx.Dispose();
            _conn.Dispose();
        }

        private static RegistrarProfissionalViewModel Vm(int tipo = 1, int plano = 1, string cpf = "CPF-NOVO") => new()
        {
            Email = "x@s.local", Senha = "Senha@123", ConfirmarSenha = "Senha@123",
            IdTipoProfissional = tipo, Nome = "Nome", Cpf = cpf,
            Numero = "1", Bairro = "Centro", Cep = "69000-000", IdCidade = 1, IdPlano = plano
        };

        [Fact]
        public async Task RegistrarAsync_PlanoIncompativelComTipo_Falha_SemCriarUsuario()
        {
            // Médico (tipo 1) escolhendo plano de Nutricionista (Nutri Total = id 2).
            var r = await _svc.RegistrarAsync(Vm(tipo: 1, plano: 2));

            Assert.False(r.Sucesso);
            Assert.Contains(r.Erros, e => e.Campo == nameof(RegistrarProfissionalViewModel.IdPlano));
            _userMgr.Verify(m => m.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()), Times.Never);
            Assert.False(await _ctx.TbProfissional.AnyAsync());
        }

        [Fact]
        public async Task RegistrarAsync_CpfDuplicado_Falha()
        {
            TestDb.CriarProfissional(_ctx, "existente"); // gera CPF-existente
            var cpf = await _ctx.TbProfissional.Select(p => p.Cpf).FirstAsync();

            var r = await _svc.RegistrarAsync(Vm(cpf: cpf));

            Assert.False(r.Sucesso);
            Assert.Contains(r.Erros, e => e.Campo == nameof(RegistrarProfissionalViewModel.Cpf));
            _userMgr.Verify(m => m.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RegistrarAsync_Sucesso_CriaProfissionalContratoEAtribuiRole()
        {
            var r = await _svc.RegistrarAsync(Vm(tipo: 1, plano: 1, cpf: "CPF-OK"));

            Assert.True(r.Sucesso);
            Assert.NotNull(r.Usuario);
            var prof = await _ctx.TbProfissional.FirstOrDefaultAsync(p => p.Cpf == "CPF-OK");
            Assert.NotNull(prof);
            Assert.Equal(1, prof!.IdTipoProfissional);
            Assert.True(await _ctx.TbContrato.AnyAsync(c => c.IdContrato == prof.IdContrato));
            _userMgr.Verify(m => m.AddToRoleAsync(It.IsAny<IdentityUser>(), DbInitializer.RoleMedico), Times.Once);
        }

        [Fact]
        public async Task RegistrarAsync_CreateAsyncFalha_RetornaErrosSemProfissional()
        {
            _userMgr.Setup(m => m.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Senha fraca." }));

            var r = await _svc.RegistrarAsync(Vm());

            Assert.False(r.Sucesso);
            Assert.Contains(r.Erros, e => e.Mensagem == "Senha fraca.");
            Assert.False(await _ctx.TbProfissional.AnyAsync());
        }

        [Fact]
        public async Task RegistrarAsync_FalhaAoAtribuirRole_CompensaRemovendoUsuario()
        {
            _userMgr.Setup(m => m.AddToRoleAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "erro role" }));

            var r = await _svc.RegistrarAsync(Vm());

            Assert.False(r.Sucesso);
            Assert.False(await _ctx.TbProfissional.AnyAsync()); // nada persistido
            _userMgr.Verify(m => m.DeleteAsync(It.IsAny<IdentityUser>()), Times.Once); // compensação
        }
    }
}
