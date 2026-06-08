using System.ComponentModel.DataAnnotations;

// Lucas Wilman da Silva Crispim
namespace Projeto1_Web2_IF_Lucas.Models
{
    public class Paciente
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório.")]
        [Display(Name = "Nome")]
        [StringLength(120)]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O CPF é obrigatório.")]
        [Display(Name = "CPF")]
        [StringLength(14)]
        public string Cpf { get; set; } = string.Empty;

        [Display(Name = "Data de Nascimento")]
        [DataType(DataType.Date)]
        public DateTime DataNascimento { get; set; }

        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        [Display(Name = "E-mail")]
        [StringLength(120)]
        public string? Email { get; set; }

        [Display(Name = "Telefone")]
        [StringLength(20)]
        public string? Telefone { get; set; }
    }
}
