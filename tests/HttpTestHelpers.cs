using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Projeto1_Web2_IF_Lucas.Tests
{
    // Helpers para os testes de integração: extração do token antiforgery e fluxos de
    // registro/login via formulários (form-urlencoded), como um navegador faria.
    public static class HttpTestHelpers
    {
        private static readonly Regex TokenRegex =
            new(@"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""", RegexOptions.Compiled);

        public static async Task<string> ObterTokenAsync(this HttpClient client, string url)
        {
            var html = await client.GetStringAsync(url);
            var m = TokenRegex.Match(html);
            return m.Success ? m.Groups[1].Value : string.Empty;
        }

        private static FormUrlEncodedContent Form(IEnumerable<KeyValuePair<string, string>> dados)
            => new(dados);

        // POST genérico: busca o token antiforgery em urlToken e envia o formulário a urlPost.
        public static async Task<HttpResponseMessage> PostComTokenAsync(
            this HttpClient client, string urlToken, string urlPost, Dictionary<string, string> campos)
        {
            campos["__RequestVerificationToken"] = await client.ObterTokenAsync(urlToken);
            return await client.PostAsync(urlPost, Form(campos));
        }

        // Registra um profissional (Médico tipo=1 / Nutricionista tipo=2). Retorna a resposta
        // do POST (302 em caso de sucesso). O client fica autenticado (cookie) após sucesso.
        public static async Task<HttpResponseMessage> RegistrarProfissionalAsync(
            this HttpClient client, int tipo, string email, string nome, string cpf, int idPlano,
            int idCidade = 1, string senha = "Senha@123")
        {
            var token = await client.ObterTokenAsync("/Conta/Registrar");
            var dados = new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["IdTipoProfissional"] = tipo.ToString(),
                ["Email"] = email,
                ["Senha"] = senha,
                ["ConfirmarSenha"] = senha,
                ["Nome"] = nome,
                ["Cpf"] = cpf,
                ["Numero"] = "1",
                ["Bairro"] = "Centro",
                ["Cep"] = "69000-000",
                ["IdCidade"] = idCidade.ToString(),
                ["IdPlano"] = idPlano.ToString()
            };
            return await client.PostAsync("/Conta/Registrar", Form(dados));
        }

        // Faz login pela página do Identity. Retorna a resposta do POST (302 em sucesso).
        public static async Task<HttpResponseMessage> LoginAsync(
            this HttpClient client, string email, string senha)
        {
            var token = await client.ObterTokenAsync("/Identity/Account/Login");
            var dados = new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Input.Email"] = email,
                ["Input.Password"] = senha,
                ["Input.RememberMe"] = "false"
            };
            return await client.PostAsync("/Identity/Account/Login", Form(dados));
        }

        // Cria um paciente para o profissional logado. Retorna a resposta do POST.
        public static async Task<HttpResponseMessage> CriarPacienteAsync(
            this HttpClient client, string nome, string cpf, int idCidade = 1,
            string informacaoResumida = "")
        {
            var token = await client.ObterTokenAsync("/MeusPacientes/Criar");
            var rg = ("RG" + cpf);
            if (rg.Length > 15) rg = rg.Substring(0, 15); // respeita StringLength(15)
            var dados = new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Nome"] = nome,
                ["Rg"] = rg,
                ["Cpf"] = cpf,
                ["DataNascimento"] = "1990-05-20",
                ["Sexo"] = "M",
                ["Etnia"] = "1",
                ["IdCidade"] = idCidade.ToString(),
                ["informacaoResumida"] = informacaoResumida
            };
            return await client.PostAsync("/MeusPacientes/Criar", Form(dados));
        }
    }
}
