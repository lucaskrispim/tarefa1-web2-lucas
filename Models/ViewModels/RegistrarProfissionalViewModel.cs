// Lucas Wilman da Silva Crispim
// ViewModel do auto-cadastro do profissional (Trabalho Final).
// Junta, numa única tela (Registro), os dados de login + o tipo (Médico/Nutricionista)
// + os dados do profissional, conforme o enunciado.
#nullable disable
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Projeto1_Web2_IF_Lucas.Models.ViewModels
{
    public class RegistrarProfissionalViewModel
    {
        // ---- Conta (Identity) ----
        [Required(ErrorMessage = "Informe o e-mail.")]
        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        [Display(Name = "E-mail")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Informe a senha.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter ao menos {2} caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha")]
        public string Senha { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar senha")]
        [Compare(nameof(Senha), ErrorMessage = "A confirmação não confere com a senha.")]
        public string ConfirmarSenha { get; set; }

        // ---- Tipo do profissional (1 = Médico, 2 = Nutricionista) ----
        [Required(ErrorMessage = "Escolha o tipo de profissional.")]
        [Display(Name = "Tipo de profissional")]
        public int IdTipoProfissional { get; set; }

        // ---- Dados do profissional ----
        [Required(ErrorMessage = "Informe o nome.")]
        [StringLength(100)]
        [Display(Name = "Nome")]
        public string Nome { get; set; }

        [Required(ErrorMessage = "Informe o CPF.")]
        [StringLength(15)]
        [Display(Name = "CPF")]
        public string Cpf { get; set; }

        [StringLength(20)]
        [Display(Name = "CRM / CRN")]
        public string CrmCrn { get; set; }

        [StringLength(100)]
        [Display(Name = "Especialidade")]
        public string Especialidade { get; set; }

        [StringLength(100)]
        [Display(Name = "Logradouro")]
        public string Logradouro { get; set; }

        [Required(ErrorMessage = "Informe o número.")]
        [StringLength(10)]
        [Display(Name = "Número")]
        public string Numero { get; set; }

        [Required(ErrorMessage = "Informe o bairro.")]
        [StringLength(100)]
        [Display(Name = "Bairro")]
        public string Bairro { get; set; }

        [Required(ErrorMessage = "Informe o CEP.")]
        [StringLength(10)]
        [Display(Name = "CEP")]
        public string Cep { get; set; }

        [StringLength(2)]
        [Display(Name = "UF")]
        public string Estado { get; set; }

        [Required(ErrorMessage = "Selecione a cidade.")]
        [Display(Name = "Cidade")]
        public int IdCidade { get; set; }

        [StringLength(2)]
        [Display(Name = "DDD")]
        public string Ddd1 { get; set; }

        [StringLength(25)]
        [Display(Name = "Telefone")]
        public string Telefone1 { get; set; }

        // ---- Plano (contrato) ----
        [Required(ErrorMessage = "Selecione o plano.")]
        [Display(Name = "Plano")]
        public int IdPlano { get; set; }

        // ---- Apoio para a view (não vêm do formulário) ----
        public SelectList Cidades { get; set; }
        public IEnumerable<TipoOption> Tipos { get; set; } = new List<TipoOption>();
        public IEnumerable<PlanoOption> Planos { get; set; } = new List<PlanoOption>();

        public record TipoOption(int Id, string Nome);

        // TipoId associa o plano ao tipo de profissional por convenção de nome,
        // permitindo filtrar no cliente os planos do tipo escolhido (item extra).
        public record PlanoOption(int Id, string Nome, decimal Valor, int TipoId);
    }
}
