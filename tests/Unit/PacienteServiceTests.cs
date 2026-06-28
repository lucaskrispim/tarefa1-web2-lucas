using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Projeto1_Web2_IF_Lucas.Models;
using Projeto1_Web2_IF_Lucas.Services;
using Xunit;

namespace Projeto1_Web2_IF_Lucas.Tests.Unit
{
    // Testes UNITÁRIOS do PacienteService (só db_ifContext, sem subir o app).
    public class PacienteServiceTests : IDisposable
    {
        private readonly db_ifContext _ctx;
        private readonly SqliteConnection _conn;
        private readonly PacienteService _svc;

        public PacienteServiceTests()
        {
            (_ctx, _conn) = TestDb.Criar();
            _svc = new PacienteService(_ctx);
        }

        public void Dispose()
        {
            _ctx.Dispose();
            _conn.Dispose();
        }

        private static TbPaciente NovoPaciente(string nome) => new()
        {
            Nome = nome, Rg = "RG", Cpf = "CPF-" + nome, Sexo = "M",
            Etnia = 1, DataNascimento = new DateOnly(1990, 1, 1)
        };

        [Fact]
        public async Task CriarAsync_PersistePacienteEVinculo()
        {
            var idProf = TestDb.CriarProfissional(_ctx, "user-a");

            await _svc.CriarAsync(NovoPaciente("Joao"), idProf, "anotacao");

            var pac = await _ctx.TbPaciente.FirstOrDefaultAsync(p => p.Nome == "Joao");
            Assert.NotNull(pac);
            var vinc = await _ctx.TbMedicoPaciente.FirstOrDefaultAsync(mp => mp.IdProfissional == idProf);
            Assert.NotNull(vinc);
            Assert.Equal("anotacao", vinc!.InformacaoResumida);
        }

        [Fact]
        public async Task ObterDoProfissionalAsync_SoRetornaSeVinculado()
        {
            var profA = TestDb.CriarProfissional(_ctx, "a");
            var profB = TestDb.CriarProfissional(_ctx, "b");
            var idPac = TestDb.CriarPacienteVinculado(_ctx, profA, "PacA");

            Assert.NotNull(await _svc.ObterDoProfissionalAsync(idPac, profA, false));
            Assert.Null(await _svc.ObterDoProfissionalAsync(idPac, profB, false));
        }

        [Fact]
        public async Task ListarDoProfissionalAsync_RetornaSoOsDoProfissional()
        {
            var profA = TestDb.CriarProfissional(_ctx, "a");
            var profB = TestDb.CriarProfissional(_ctx, "b");
            TestDb.CriarPacienteVinculado(_ctx, profA, "DaA");
            TestDb.CriarPacienteVinculado(_ctx, profB, "DaB");

            var lista = await _svc.ListarDoProfissionalAsync(profA);
            Assert.Single(lista);
            Assert.Equal("DaA", lista[0].Nome);
        }

        [Fact]
        public async Task ExcluirDoProfissionalAsync_RemoveOrfao()
        {
            var prof = TestDb.CriarProfissional(_ctx, "a");
            var idPac = TestDb.CriarPacienteVinculado(_ctx, prof, "Orfao");

            var ok = await _svc.ExcluirDoProfissionalAsync(idPac, prof);

            Assert.True(ok);
            Assert.False(await _ctx.TbPaciente.AnyAsync(p => p.IdPaciente == idPac));
            Assert.False(await _ctx.TbMedicoPaciente.AnyAsync(mp => mp.IdPaciente == idPac));
        }

        [Fact]
        public async Task ExcluirDoProfissionalAsync_MantemPacienteCompartilhado()
        {
            var profA = TestDb.CriarProfissional(_ctx, "a");
            var profB = TestDb.CriarProfissional(_ctx, "b");
            var idPac = TestDb.CriarPacienteVinculado(_ctx, profA, "Compart");
            _ctx.TbMedicoPaciente.Add(new TbMedicoPaciente { IdProfissional = profB, IdPaciente = idPac, InformacaoResumida = "x" });
            await _ctx.SaveChangesAsync();

            var ok = await _svc.ExcluirDoProfissionalAsync(idPac, profA);

            Assert.True(ok);
            Assert.True(await _ctx.TbPaciente.AnyAsync(p => p.IdPaciente == idPac)); // preservado
            Assert.False(await _ctx.TbMedicoPaciente.AnyAsync(mp => mp.IdPaciente == idPac && mp.IdProfissional == profA));
            Assert.True(await _ctx.TbMedicoPaciente.AnyAsync(mp => mp.IdPaciente == idPac && mp.IdProfissional == profB));
        }

        [Fact]
        public async Task ExcluirDoProfissionalAsync_RetornaFalseSeNaoForDele()
        {
            var profA = TestDb.CriarProfissional(_ctx, "a");
            var profB = TestDb.CriarProfissional(_ctx, "b");
            var idPac = TestDb.CriarPacienteVinculado(_ctx, profA, "Pac");

            Assert.False(await _svc.ExcluirDoProfissionalAsync(idPac, profB));
            Assert.True(await _ctx.TbPaciente.AnyAsync(p => p.IdPaciente == idPac)); // intacto
        }
    }
}
