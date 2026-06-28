// Lucas Wilman da Silva Crispim
// Trabalho Final - extensão (partial) do db_ifContext com as entidades de negócio
// necessárias ao trabalho final, sem alterar o arquivo gerado db_ifContext.cs.
#nullable disable
using Microsoft.EntityFrameworkCore;

namespace Projeto1_Web2_IF_Lucas.Models;

public partial class db_ifContext
{
    public virtual DbSet<TbProfissional> TbProfissional { get; set; }

    public virtual DbSet<TbTipoProfissional> TbTipoProfissional { get; set; }

    public virtual DbSet<TbContrato> TbContrato { get; set; }

    public virtual DbSet<TbPlano> TbPlano { get; set; }

    public virtual DbSet<TbMedicoPaciente> TbMedicoPaciente { get; set; }

    // Configuração adicional via hook partial (chamado pelo OnModelCreating gerado).
    // As relações já são expressas por data annotations nas entidades; aqui apenas
    // reforçamos o comportamento de exclusão (Restrict) para evitar deleção em cascata
    // acidental de profissionais/pacientes.
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TbMedicoPaciente>(entity =>
        {
            entity.HasOne(d => d.IdPacienteNavigation)
                .WithMany(p => p.TbMedicoPaciente)
                .HasForeignKey(d => d.IdPaciente)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_tbMedico_Paciente_tbPaciente");

            entity.HasOne(d => d.IdProfissionalNavigation)
                .WithMany(p => p.TbMedicoPaciente)
                .HasForeignKey(d => d.IdProfissional)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_tbMedico_Paciente_tbProfissional");
        });

        modelBuilder.Entity<TbProfissional>(entity =>
        {
            entity.HasOne(d => d.IdContratoNavigation)
                .WithMany(p => p.TbProfissional)
                .HasForeignKey(d => d.IdContrato)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_tbProfissional_tbContrato");

            entity.HasOne(d => d.IdTipoProfissionalNavigation)
                .WithMany(p => p.TbProfissional)
                .HasForeignKey(d => d.IdTipoProfissional)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tbProfissional_tbTipoProfissional");

            entity.HasOne(d => d.IdCidadeNavigation)
                .WithMany()
                .HasForeignKey(d => d.IdCidade)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_tbProfissional_tbCidade");
        });

        modelBuilder.Entity<TbContrato>(entity =>
        {
            entity.HasOne(d => d.IdPlanoNavigation)
                .WithMany(p => p.TbContrato)
                .HasForeignKey(d => d.IdPlano)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_tbContrato_tbPlano");
        });
    }
}
