using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Projeto1_Web2_IF_Lucas.Models;
using Projeto1_Web2_IF_Lucas.Services;
using Xunit;

namespace Projeto1_Web2_IF_Lucas.Tests.Unit
{
    // Testes UNITÁRIOS do ProfissionalService (db_ifContext in-memory + UserManager mockado).
    public class ProfissionalServiceTests : IDisposable
    {
        private readonly db_ifContext _ctx;
        private readonly SqliteConnection _conn;
        private readonly Mock<UserManager<IdentityUser>> _userMgr;
        private readonly ProfissionalService _svc;

        public ProfissionalServiceTests()
        {
            (_ctx, _conn) = TestDb.Criar();
            _userMgr = TestDb.MockUserManager();
            _userMgr.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(new IdentityUser());
            _userMgr.Setup(m => m.DeleteAsync(It.IsAny<IdentityUser>())).ReturnsAsync(IdentityResult.Success);
            _svc = new ProfissionalService(_ctx, _userMgr.Object, NullLogger<ProfissionalService>.Instance);
        }

        public void Dispose()
        {
            _ctx.Dispose();
            _conn.Dispose();
        }

        [Fact]
        public async Task ListarPorEscopoAsync_FiltraPorTipo()
        {
            TestDb.CriarProfissional(_ctx, "med", idTipo: 1, idPlano: 1);
            TestDb.CriarProfissional(_ctx, "nut", idTipo: 2, idPlano: 2);

            Assert.Single(await _svc.ListarPorEscopoAsync(1));   // só médicos
            Assert.Single(await _svc.ListarPorEscopoAsync(2));   // só nutricionistas
            Assert.Equal(2, (await _svc.ListarPorEscopoAsync(null)).Count); // todos
        }

        [Fact]
        public async Task ExcluirAsync_BloqueiaComPacientesSemCascata()
        {
            var idProf = TestDb.CriarProfissional(_ctx, "a");
            TestDb.CriarPacienteVinculado(_ctx, idProf, "Pac");

            var r = await _svc.ExcluirAsync(idProf, cascata: false);

            Assert.Equal(StatusExclusao.BloqueadoComPacientes, r.Status);
            Assert.True(await _ctx.TbProfissional.AnyAsync(p => p.IdProfissional == idProf));
            _userMgr.Verify(m => m.DeleteAsync(It.IsAny<IdentityUser>()), Times.Never);
        }

        [Fact]
        public async Task ExcluirAsync_SemPacientes_RemoveProfContratoELogin()
        {
            var idProf = TestDb.CriarProfissional(_ctx, "a");
            var idContrato = await _ctx.TbProfissional.Where(p => p.IdProfissional == idProf)
                .Select(p => p.IdContrato).FirstAsync();

            var r = await _svc.ExcluirAsync(idProf, cascata: false);

            Assert.Equal(StatusExclusao.Sucesso, r.Status);
            Assert.False(await _ctx.TbProfissional.AnyAsync(p => p.IdProfissional == idProf));
            Assert.False(await _ctx.TbContrato.AnyAsync(c => c.IdContrato == idContrato));
            _userMgr.Verify(m => m.DeleteAsync(It.IsAny<IdentityUser>()), Times.Once);
        }

        [Fact]
        public async Task ExcluirAsync_CascataRemoveExclusivos()
        {
            var idProf = TestDb.CriarProfissional(_ctx, "a");
            var idPac = TestDb.CriarPacienteVinculado(_ctx, idProf, "Excl");

            var r = await _svc.ExcluirAsync(idProf, cascata: true);

            Assert.Equal(StatusExclusao.Sucesso, r.Status);
            Assert.Equal(1, r.PacientesExclusivosRemovidos);
            Assert.False(await _ctx.TbPaciente.AnyAsync(p => p.IdPaciente == idPac));
        }

        [Fact]
        public async Task ExcluirAsync_CascataPreservaCompartilhado()
        {
            var profA = TestDb.CriarProfissional(_ctx, "a");
            var profB = TestDb.CriarProfissional(_ctx, "b");
            var idPac = TestDb.CriarPacienteVinculado(_ctx, profA, "Comp");
            _ctx.TbMedicoPaciente.Add(new TbMedicoPaciente { IdProfissional = profB, IdPaciente = idPac, InformacaoResumida = "x" });
            await _ctx.SaveChangesAsync();

            var r = await _svc.ExcluirAsync(profA, cascata: true);

            Assert.Equal(StatusExclusao.Sucesso, r.Status);
            Assert.Equal(0, r.PacientesExclusivosRemovidos);
            Assert.True(await _ctx.TbPaciente.AnyAsync(p => p.IdPaciente == idPac)); // preservado
            Assert.True(await _ctx.TbMedicoPaciente.AnyAsync(mp => mp.IdProfissional == profB));
        }

        [Fact]
        public async Task ExcluirAsync_NaoEncontrado()
        {
            var r = await _svc.ExcluirAsync(9999, cascata: false);
            Assert.Equal(StatusExclusao.NaoEncontrado, r.Status);
        }

        [Fact]
        public async Task ExcluirAsync_LoginFalha_RetornaAviso()
        {
            _userMgr.Setup(m => m.DeleteAsync(It.IsAny<IdentityUser>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "erro" }));
            var idProf = TestDb.CriarProfissional(_ctx, "a");

            var r = await _svc.ExcluirAsync(idProf, cascata: false);

            Assert.Equal(StatusExclusao.SucessoComAvisoLogin, r.Status);
            Assert.False(await _ctx.TbProfissional.AnyAsync(p => p.IdProfissional == idProf)); // removido do db_if
        }

        [Fact]
        public async Task SalvarComCidadeAsync_DerivaCidadeDeIdCidade()
        {
            var idProf = TestDb.CriarProfissional(_ctx, "a");
            var prof = await _ctx.TbProfissional.FirstAsync(p => p.IdProfissional == idProf);
            prof.Cidade = null;

            await _svc.SalvarComCidadeAsync(prof);

            Assert.Equal("Manaus", prof.Cidade);
        }

        [Fact]
        public async Task ObterPacientesVinculadosAsync_MarcaExclusivos()
        {
            var profA = TestDb.CriarProfissional(_ctx, "a");
            var profB = TestDb.CriarProfissional(_ctx, "b");
            TestDb.CriarPacienteVinculado(_ctx, profA, "Excl");
            var idComp = TestDb.CriarPacienteVinculado(_ctx, profA, "Comp");
            _ctx.TbMedicoPaciente.Add(new TbMedicoPaciente { IdProfissional = profB, IdPaciente = idComp, InformacaoResumida = "x" });
            await _ctx.SaveChangesAsync();

            var lista = await _svc.ObterPacientesVinculadosAsync(profA);

            Assert.Equal(2, lista.Count);
            Assert.True(lista.Single(x => x.Nome == "Excl").Exclusivo);
            Assert.False(lista.Single(x => x.Nome == "Comp").Exclusivo);
        }
    }
}
