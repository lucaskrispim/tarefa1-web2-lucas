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
  **Create (com `[Bind]`), Details, Edit, EditPost, Delete e DeleteConfirmed**
  (nome do aluno comentado em cada action).
- Views de `TbPacientes` (Index, Create, Edit, Details, Delete).
- Arquivo **`jquery.validate.pt-br.js`** (tradução pt-BR), referenciado em
  `Views/Shared/_ValidationScriptsPartial.cshtml`.
- Prints do código em `prints_tarefa2/` (gerados por `gerar_prints.py`).

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
