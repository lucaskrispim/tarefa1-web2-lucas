using Projeto1_Web2_IF_Lucas.Models.ViewModels;

// Lucas Wilman da Silva Crispim
namespace Projeto1_Web2_IF_Lucas.Services
{
    // Encapsula o auto-cadastro do profissional: validação de negócio + criação de usuário,
    // role, profissional e contrato (com compensação em caso de falha parcial).
    public interface IRegistroService
    {
        // Preenche as listas de apoio da tela (cidades, tipos, planos com o tipo embutido).
        Task CarregarOpcoesAsync(RegistrarProfissionalViewModel vm);

        // Executa o cadastro. Em sucesso, retorna o usuário criado (para o controller logar);
        // em falha, retorna os erros de validação (sem ter persistido conta órfã).
        Task<RegistroResultado> RegistrarAsync(RegistrarProfissionalViewModel vm);
    }
}
