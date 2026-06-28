// Lucas Wilman da Silva Crispim
// Trabalho Final - entidade do banco db_if (escrita a mão, mantendo o design original).
#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Projeto1_Web2_IF_Lucas.Models;

// Plano contratado pelo profissional (ex.: Médico Total, Médico Parcial).
[Table("tbPlano")]
public partial class TbPlano
{
    [Key]
    public int IdPlano { get; set; }

    [Required]
    [StringLength(100)]
    [Unicode(false)]
    public string Nome { get; set; }

    // Validade do plano em dias/meses (conforme o cadastro original).
    public int Validade { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal Valor { get; set; }

    [InverseProperty("IdPlanoNavigation")]
    public virtual ICollection<TbContrato> TbContrato { get; set; } = new List<TbContrato>();
}
