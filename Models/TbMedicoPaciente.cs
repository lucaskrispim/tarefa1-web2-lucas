// Lucas Wilman da Silva Crispim
// Trabalho Final - entidade do banco db_if (escrita a mão, mantendo o design original).
#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Projeto1_Web2_IF_Lucas.Models;

// Tabela de ligação profissional <-> paciente. É por ela que cada profissional
// enxerga apenas os pacientes que ele mesmo cadastrou.
[Table("tbMedico_Paciente")]
[Index("IdPaciente", Name = "IX_tbMedico_Paciente_IdPaciente")]
[Index("IdProfissional", Name = "IX_tbMedico_Paciente_IdProfissional")]
public partial class TbMedicoPaciente
{
    [Key]
    [Column("IdMedico_Paciente")]
    public int IdMedicoPaciente { get; set; }

    public int IdPaciente { get; set; }

    public int IdProfissional { get; set; }

    [StringLength(5000)]
    [Unicode(false)]
    public string InformacaoResumida { get; set; }

    [ForeignKey("IdPaciente")]
    [InverseProperty("TbMedicoPaciente")]
    public virtual TbPaciente IdPacienteNavigation { get; set; }

    [ForeignKey("IdProfissional")]
    [InverseProperty("TbMedicoPaciente")]
    public virtual TbProfissional IdProfissionalNavigation { get; set; }
}
