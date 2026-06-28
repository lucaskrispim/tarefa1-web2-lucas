using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Projeto1_Web2_IF_Lucas.Models;

// Lucas Wilman da Silva Crispim
namespace Projeto1_Web2_IF_Lucas.Data
{
    // Contexto do ASP.NET Core Identity (usuários e roles).
    // Herda de IdentityDbContext para criar as tabelas AspNet* (Identity).
    // Mantém também o DbSet<Paciente> da Tarefa 1 (Code First) por histórico.
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Paciente> Pacientes { get; set; }
    }
}
