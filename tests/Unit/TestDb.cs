using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Projeto1_Web2_IF_Lucas.Models;

namespace Projeto1_Web2_IF_Lucas.Tests.Unit
{
    // Infraestrutura para testes UNITÁRIOS dos serviços: um db_ifContext sobre SQLite
    // in-memory (rápido e isolado) já semeado, e um UserManager mockado (Moq).
    public static class TestDb
    {
        // Cria um db_ifContext in-memory com schema criado e dados de apoio (tipos, cidade, planos).
        // A conexão precisa ficar aberta enquanto o contexto viver (in-memory do SQLite).
        public static (db_ifContext ctx, SqliteConnection conn) Criar()
        {
            var conn = new SqliteConnection("DataSource=:memory:");
            conn.Open();
            var options = new DbContextOptionsBuilder<db_ifContext>().UseSqlite(conn).Options;
            var ctx = new db_ifContext(options);
            ctx.Database.EnsureCreated();

            // Tipos: inseridos nesta ordem -> ids 1 (Médico) e 2 (Nutricionista).
            ctx.TbTipoProfissional.AddRange(
                new TbTipoProfissional { Nome = "Médico" },
                new TbTipoProfissional { Nome = "Nutricionista" });
            ctx.TbCidade.Add(new TbCidade { Nome = "Manaus" });
            ctx.TbPlano.AddRange(
                new TbPlano { Nome = "Médico Total", Validade = 365, Valor = 1000m },
                new TbPlano { Nome = "Nutri Total", Validade = 365, Valor = 800m });
            ctx.SaveChanges();
            return (ctx, conn);
        }

        // Cria um profissional (com contrato) e retorna o seu IdProfissional.
        public static int CriarProfissional(db_ifContext ctx, string idUser, int idTipo = 1, int idPlano = 1)
        {
            var prof = new TbProfissional
            {
                IdUser = idUser,
                Nome = "Prof " + idUser,
                Cpf = "CPF-" + idUser,
                Numero = "1",
                Bairro = "Centro",
                Cep = "69000-000",
                IdCidade = 1,
                IdTipoProfissional = idTipo,
                IdContratoNavigation = new TbContrato
                {
                    IdPlano = idPlano,
                    DataInicio = new DateTime(2026, 1, 1),
                    DataFim = new DateTime(2027, 1, 1)
                }
            };
            ctx.TbProfissional.Add(prof);
            ctx.SaveChanges();
            return prof.IdProfissional;
        }

        // Cria um paciente vinculado a um profissional e retorna o IdPaciente.
        public static int CriarPacienteVinculado(db_ifContext ctx, int idProfissional, string nome)
        {
            var vinc = new TbMedicoPaciente
            {
                IdProfissional = idProfissional,
                InformacaoResumida = "n",
                IdPacienteNavigation = new TbPaciente
                {
                    Nome = nome,
                    Rg = "RG1",
                    Cpf = "PAC-" + nome,
                    Sexo = "M",
                    Etnia = 1,
                    DataNascimento = new DateOnly(1990, 1, 1)
                }
            };
            ctx.TbMedicoPaciente.Add(vinc);
            ctx.SaveChanges();
            return vinc.IdPaciente;
        }

        public static Mock<UserManager<IdentityUser>> MockUserManager()
        {
            var store = new Mock<IUserStore<IdentityUser>>();
            return new Mock<UserManager<IdentityUser>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        }
    }
}
