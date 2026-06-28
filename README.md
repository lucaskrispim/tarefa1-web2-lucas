# Trabalho Final — Programação Web II (em desenvolvimento)

**Aluno:** Lucas Wilman da Silva Crispim

Sistema da Central de Saúde sobre o banco **`db_if`** (design mantido). Desenvolvimento em
**SQLite** (`db_if.sqlite`), criado por migrations no startup. Plano completo em
[`PLANO_TRABALHO_FINAL.md`](PLANO_TRABALHO_FINAL.md).

## Como rodar

```bash
dotnet run
```

O banco SQLite é criado e populado (seed) automaticamente no primeiro start.

## Usuários Gerentes (criados via seed, direto no banco)

> O Requisito 2 pede os 3 gerentes com as Roles **associadas diretamente no banco**. Isso é feito
> por um seed idempotente no startup (`Data/DbInitializer.cs`) usando `RoleManager`/`UserManager` —
> equivale a um script SQL, porém respeitando o hash de senha do ASP.NET Core Identity. Senha de
> todos: **`Gerente@123`** (credenciais de demonstração).

| Login | Role | Acesso |
|-------|------|--------|
| `gerentegeral@saude.local` | `GerenteGeral` | Todos os profissionais |
| `gerentemedico@saude.local` | `GerenteMedico` | Só profissionais Médicos |
| `gerentenutri@saude.local` | `GerenteNutricionista` | Só profissionais Nutricionistas |

Profissionais (Médico/Nutricionista) se **auto-registram** em `/Identity/Account/Register`.
Gerentes existem **apenas** via seed (não há registro de gerente).

## Funcionalidades extras implementadas

- **Planos por tipo:** no registro, só aparecem os planos do tipo escolhido (Médico/Nutri),
  validado também no servidor.
- **Exclusão em cascata (opcional):** ao excluir um profissional que tem pacientes, o gerente
  pode optar pela exclusão em cascata, que remove o profissional **e os pacientes exclusivos**
  (vinculados só a ele); pacientes compartilhados com outro profissional são **preservados**
  (remove-se apenas o vínculo). *Limitação conhecida:* a conta de login (Identity) é removida
  fora da transação do `db_if`; se essa etapa falhar, o gerente é avisado para removê-la manualmente.

## Arquitetura

A lógica de negócio fica numa **camada de serviço** (`Services/`: `IRegistroService`,
`IProfissionalService`, `IPacienteService`), deixando os controllers finos (só HTTP: ModelState,
`TryUpdateModelAsync`, autenticação, autorização por Role e redirecionamentos). Os serviços
retornam tipos de resultado (`RegistroResultado`, `ExclusaoResultado`) em vez de depender de
detalhes web. Registro via DI com tempo de vida *Scoped* (mesmo `DbContext` por request).

## Testes

Bateria em `tests/` com **48 testes** (`dotnet test`):

- **Integração** (xUnit + `WebApplicationFactory`, SQLite temporário): sobe o app real e cobre os
  3 requisitos e as regras de segurança (registro, ownership de pacientes, escopo dos gerentes,
  CPF não-editável pelo profissional, exclusão e cascata).
- **Unitários** (xUnit + SQLite in-memory + Moq): testam os serviços isoladamente
  (`tests/Unit/`) — validação, compensação, escopo, cascata e ownership.

```bash
dotnet test
```

---

# Projeto1_Web2_IF_Lucas — Tarefa de acompanhamento 1

**Aluno:** Lucas Wilman da Silva Crispim
**Disciplina:** Programação Web II
**Tarefa:** Tarefa de acompanhamento 1

## Sobre

Aplicação web criada do zero seguindo as etapas da aula 1: um projeto **ASP.NET Core MVC (.NET 8)**
com **Entity Framework Core** e um **controller gerado (scaffolding) a partir de um model**, com as
operações de CRUD (listar, criar, detalhar, editar e excluir).

O model escolhido foi **`Paciente`** (tema saúde), e o controller correspondente é o
**`PacientesController`**, que possui o nome do aluno no comentário acima do `namespace`,
conforme exigido na tarefa.

## O que foi feito

- Projeto ASP.NET Core MVC (.NET 8) criado do zero.
- Configuração do **Entity Framework Core** com banco de dados **SQLite** (`db_if.sqlite`),
  criado automaticamente no primeiro start via migrations.
- Model `Paciente` com validações (`Nome`, `CPF`, `Data de Nascimento`, `E-mail`, `Telefone`).
- `ApplicationDbContext` registrando o `DbSet<Paciente>`.
- Controller **`PacientesController`** gerado via scaffolding, com CRUD completo e as Views correspondentes.
- Link "Pacientes" adicionado ao menu de navegação.

## Estrutura do projeto

```
Projeto1_Web2_IF_Lucas/
├── Controllers/
│   ├── HomeController.cs
│   └── PacientesController.cs    # CRUD + nome do aluno acima do namespace
├── Models/
│   └── Paciente.cs               # model
├── Data/
│   └── ApplicationDbContext.cs   # DbContext (EF Core)
├── Migrations/                   # migration InitialCreate
├── Views/
│   └── Pacientes/                # Index, Create, Edit, Details, Delete
├── Program.cs                    # configuração EF + SQLite + Migrate no startup
├── appsettings.json              # connection string (SQLite)
└── Projeto1_Web2_IF_Lucas.csproj
```

## Como executar

Pré-requisito: **.NET 8 SDK** instalado.

```bash
# na pasta do projeto
dotnet run
```

A aplicação sobe em `http://localhost:<porta>` (a porta é exibida no terminal).
O banco SQLite é criado automaticamente no primeiro start.

Para acessar o CRUD de pacientes, abra:

```
http://localhost:<porta>/Pacientes
```

## Tecnologias

- ASP.NET Core MVC (.NET 8)
- Entity Framework Core 8
- SQLite
- Bootstrap (template padrão MVC)

---

# Tarefa de acompanhamento 2 — TbPaciente (Database First)

**Aluno:** Lucas Wilman da Silva Crispim

Nesta tarefa o trabalho foi feito **conforme o vídeo da aula (Database First)**: os models
foram obtidos por **engenharia reversa** do banco **`db_if`** (SQL Server) usando o
**EF Core Power Tools** (ver `efpt.config.json`), gerando o `db_ifContext` e o model `TbPaciente`
(com a relação para `TbCidade`).

## O que foi feito

- Model `TbPaciente` e `TbCidade` no padrão gerado pelo EF Core Power Tools (`Models/`).
- Contexto `db_ifContext` (`Models/db_ifContext.cs`) usando **SQL Server** (banco `db_if`).
- Controller **`TbPacientesController`** com os actions vistos nas aulas:
  **Create, Details, Edit, EditPost, Delete e DeleteConfirmed**
  (nome do aluno comentado em cada action).
- Views de `TbPacientes` (Index, Create, Edit, Details, Delete).
- Arquivo **`jquery.validate.pt-br.js`** (tradução pt-BR), referenciado em
  `Views/Shared/_ValidationScriptsPartial.cshtml`.
- Prints do código em `prints_tarefa2/` (gerados por `gerar_prints.py`).

## Correções aplicadas (revisão do professor)

Seguindo o tutorial indicado no enunciado e mostrado na aula 3
([Microsoft – CRUD com EF Core](https://learn.microsoft.com/en-us/aspnet/core/data/ef-mvc/crud?view=aspnetcore-8.0)):

- **Tratamento de exceções (`try-catch` com `DbUpdateException`)** nos métodos que
  gravam no banco:
  - `Create` — em caso de falha ao salvar, adiciona erro ao `ModelState` e re-exibe a view.
  - `EditPost` — `try-catch` em volta do `SaveChangesAsync`.
  - `DeleteConfirmed` — `try-catch`; se falhar, redireciona para `Delete` com
    `saveChangesError = true`, e o `Delete` (GET) exibe a mensagem via
    `ViewData["ErrorMessage"]` na view `Delete.cshtml`.
- **`TryUpdateModelAsync` no Edit (POST), no lugar de `[Bind]`:** o `EditPost` agora
  carrega a entidade do banco (`FirstOrDefaultAsync`) e usa
  `TryUpdateModelAsync<TbPaciente>` listando explicitamente as propriedades atualizáveis.
  Essa é a abordagem recomendada no tutorial/aula — protege contra *overposting* sem
  depender do `[Bind]`.

## Ligação com o banco de dados

A connection string fica em `appsettings.json`:

```json
"DbIfConnection": "Server=localhost;Database=db_if;Integrated Security=True;TrustServerCertificate=true;"
```

Ajuste `Server=` para o nome da sua instância do SQL Server. O `db_ifContext` é registrado
em `Program.cs` com `UseSqlServer(...)`.

### Rodar local em SQLite (sem SQL Server)

Caso você não tenha o banco `db_if` no SQL Server, há instruções comentadas em `Program.cs`
para trocar o `db_ifContext` de `UseSqlServer` para `UseSqlite` usando a `DefaultConnection`.

## Observação sobre a Tarefa 1

A Tarefa 1 havia sido feita em **Code First** (model `Paciente` + SQLite via migrations),
o que divergiu do vídeo. Nesta entrega a abordagem foi corrigida para **Database First**
(model `TbPaciente` gerado a partir do banco `db_if`). O `PacientesController`/`Paciente`
da Tarefa 1 foi mantido apenas para histórico.
