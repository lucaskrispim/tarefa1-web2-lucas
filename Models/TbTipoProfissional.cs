// Lucas Wilman da Silva Crispim
// Trabalho Final - entidade do banco db_if (escrita a mão, mantendo o design original).
#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Projeto1_Web2_IF_Lucas.Models;

// Tipo do profissional (ex.: Médico, Nutricionista).
[Table("tbTipoProfissional")]
public partial class TbTipoProfissional
{
    [Key]
    public int IdTipoProfissional { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string Nome { get; set; }

    [InverseProperty("IdTipoProfissionalNavigation")]
    public virtual ICollection<TbProfissional> TbProfissional { get; set; } = new List<TbProfissional>();
}
