// Lucas Wilman da Silva Crispim
// Trabalho Final - entidade do banco db_if (escrita a mão, mantendo o design original).
#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Projeto1_Web2_IF_Lucas.Models;

// Profissional de saúde (Médico ou Nutricionista). O vínculo com o login fica em IdUser
// (Id do AspNetUsers / Identity), validado em código - sem FK entre contextos.
[Table("tbProfissional")]
[Index("IdCidade", Name = "IX_tbProfissional_IdCidade")]
[Index("IdContrato", Name = "IX_tbProfissional_IdContrato")]
[Index("IdTipoAcesso", Name = "IX_tbProfissional_IdTipoAcesso")]
public partial class TbProfissional
{
    [Key]
    public int IdProfissional { get; set; }

    public int? IdTipoProfissional { get; set; }

    public int IdContrato { get; set; }

    // Coluna existente no design (tipo de acesso). Mantida como escalar; sem navegação
    // pois a tabela tbTipoAcesso não é modelada neste trabalho.
    public int? IdTipoAcesso { get; set; }

    public int IdCidade { get; set; }

    // Id do usuário do Identity (AspNetUsers.Id). Liga o profissional ao seu login.
    [Required]
    [StringLength(128)]
    public string IdUser { get; set; }

    [Required]
    [StringLength(100)]
    [Unicode(false)]
    public string Nome { get; set; }

    [Required]
    [Column("CPF")]
    [StringLength(15)]
    [Unicode(false)]
    public string Cpf { get; set; }

    [Column("CRM_CRN")]
    [StringLength(20)]
    [Unicode(false)]
    public string CrmCrn { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string Especialidade { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string Logradouro { get; set; }

    [Required]
    [StringLength(10)]
    [Unicode(false)]
    public string Numero { get; set; }

    [Required]
    [StringLength(100)]
    [Unicode(false)]
    public string Bairro { get; set; }

    [Required]
    [Column("CEP")]
    [StringLength(10)]
    [Unicode(false)]
    public string Cep { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string Cidade { get; set; }

    [StringLength(2)]
    [Unicode(false)]
    public string Estado { get; set; }

    [Column("DDD1")]
    [StringLength(2)]
    [Unicode(false)]
    public string Ddd1 { get; set; }

    [Column("DDD2")]
    [StringLength(2)]
    [Unicode(false)]
    public string Ddd2 { get; set; }

    [StringLength(25)]
    [Unicode(false)]
    public string Telefone1 { get; set; }

    [StringLength(25)]
    [Unicode(false)]
    public string Telefone2 { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal? Salario { get; set; }

    [ForeignKey("IdCidade")]
    public virtual TbCidade IdCidadeNavigation { get; set; }

    [ForeignKey("IdContrato")]
    [InverseProperty("TbProfissional")]
    public virtual TbContrato IdContratoNavigation { get; set; }

    [ForeignKey("IdTipoProfissional")]
    [InverseProperty("TbProfissional")]
    public virtual TbTipoProfissional IdTipoProfissionalNavigation { get; set; }

    // Pacientes vinculados a este profissional (tabela de ligação tbMedico_Paciente).
    [InverseProperty("IdProfissionalNavigation")]
    public virtual ICollection<TbMedicoPaciente> TbMedicoPaciente { get; set; } = new List<TbMedicoPaciente>();
}
