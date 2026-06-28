// Lucas Wilman da Silva Crispim
// Trabalho Final - entidade do banco db_if (escrita a mão, mantendo o design original).
#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Projeto1_Web2_IF_Lucas.Models;

// Contrato do profissional, vinculado a um plano e a um período de vigência.
[Table("tbContrato")]
[Index("IdPlano", Name = "IX_tbContrato_IdPlano")]
public partial class TbContrato
{
    [Key]
    public int IdContrato { get; set; }

    public int IdPlano { get; set; }

    public DateTime? DataInicio { get; set; }

    public DateTime? DataFim { get; set; }

    [ForeignKey("IdPlano")]
    [InverseProperty("TbContrato")]
    public virtual TbPlano IdPlanoNavigation { get; set; }

    [InverseProperty("IdContratoNavigation")]
    public virtual ICollection<TbProfissional> TbProfissional { get; set; } = new List<TbProfissional>();
}
