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
