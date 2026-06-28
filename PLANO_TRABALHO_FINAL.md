# Plano — Trabalho Final de Programação Web II

**Aluno:** Lucas Wilman da Silva Crispim
**Projeto:** `tarefa1-web2-lucas` (ASP.NET Core MVC, .NET 8)
**Banco:** `db_if` (design original, sem alterações)

> Solução escrita de forma **independente**. O projeto do colega serve apenas como referência
> de domínio/banco — código, estrutura e nomes são próprios (cópia = nota ZERO).

---

## 0. Decisão de banco — **SQLite (definido)**

Desenvolvimento em **SQLite** (`db_if.sqlite`), criado pelo próprio EF via **migrations**.
O **design do `db_if` é mantido** (mesmas tabelas, colunas e relacionamentos) — muda apenas o
motor para rodar localmente sem SQL Server.

Implicações desta escolha:
- As entidades de negócio faltantes serão **escritas em código** a partir do design conhecido do
  `db_if` (não há script `.sql` do `db_if` no repositório, e não há SQL Server para engenharia reversa).
- Tipos específicos do SQL Server precisam de mapeamento compatível com SQLite:
  - `decimal(10,2)` → ok no EF (armazenado como TEXT/REAL); manter `[Column(TypeName=...)]` só se necessário.
  - `smalldatetime` → usar `DateTime`/`DateOnly` sem `TypeName` específico de SQL Server.
  - `Unicode(false)`/`StringLength` → mantidos (SQLite ignora, mas preserva o design e valida no app).
- A app mantém **duas connection strings** (`DefaultConnection` = SQLite usada no dev;
  `DbIfConnection` = SQL Server) para que, se for preciso entregar em SQL Server, baste trocar
  o provider por configuração.
- O **Identity** e o **seed** (roles + 3 gerentes) rodam normalmente sobre o SQLite.

---

## 1. Arquitetura e boas práticas (alvo dos 2.5 pts de Codificação)

- **Dois DbContext**, ambos na mesma base:
  - `ApplicationDbContext : IdentityDbContext` → tabelas `AspNet*` (Identity).
  - `db_ifContext` → tabelas de negócio (`tbProfissional`, `tbPaciente`, `tbMedico_Paciente`, ...).
- **ASP.NET Core Identity** com `AddDefaultIdentity<IdentityUser>().AddRoles<IdentityRole>()`.
- **Autorização baseada em Roles no Controller** (`[Authorize(Roles="...")]`) — nunca só escondendo links.
- **Autorização de propriedade (ownership)**: profissional só acessa o que é dele — checagem
  explícita por `IdUser`/`IdProfissional` dentro das actions (idealmente via
  `IAuthorizationService` + um `AuthorizationHandler`, ou no mínimo checagem manual consistente).
- **ViewModels** para telas que combinam dados (ex.: Registro = login + senha + profissional).
- **EF Core assíncrono** (`async/await`, `ToListAsync`, `FirstOrDefaultAsync`).
- **Sem overposting**: `TryUpdateModelAsync` no Edit (ou ViewModel + mapeamento explícito), nunca `[Bind]` largo.
- **Tratamento de exceções**: `try-catch` com `DbUpdateException` em Create/Edit/Delete + mensagens amigáveis.
- **Nullable reference types**, **injeção de dependência**, **anti-forgery token**, indentação e comentários.

---

## Notas de design (validadas por revisão cruzada — aplicar nas próximas fases)

- **FK cross-context `TbProfissional.IdUser → AspNetUsers.Id`:** os dois contextos não
  enxergam as tabelas um do outro. Guardar `IdUser` como `string` em `TbProfissional` e
  **validar o vínculo em código** (sem FK real entre contextos). Evita migration manual frágil.
- **Migrations de negócio:** sempre `dotnet ef migrations add <Nome> --context db_ifContext
  --output-dir Migrations/DbIf`. **Nunca** re-scaffold por cima de `db_ifContext.cs`/models
  (sobrescreve ajustes e descasa do snapshot) — tratá-los como código-fonte manual a partir daqui.
- **`decimal` em `TbContrato`/`TbPlano`:** no SQLite vira TEXT (aviso de precisão em SUM/AVG).
  Aceitável em dev; só atenção se for agregar valores.
- **SQLite:** `Foreign Keys=True` ativado na connection string (FK no SQLite é off por padrão).
- **`Paciente` (Tarefa 1) x `TbPaciente` (db_if):** tabelas distintas (`Pacientes` vs `tbPaciente`),
  sem conflito. `Paciente` é mantido só por histórico da Tarefa 1 — não usar no Trabalho Final.

### Decisões travadas na Fase 2 (validação cruzada)

- **Distinção Médico x Nutricionista (filtro dos gerentes):** usar `TbProfissional.IdTipoProfissional`
  (mesmo contexto da listagem), com `tbTipoProfissional` **seedado com ids estáveis**
  (1 = Médico, 2 = Nutricionista). O registro **sempre** preenche `IdTipoProfissional`.
  A Role do Identity é o *portão de autorização*; o `IdTipoProfissional` é o *critério de filtro de dados*.
- **Registro cria contrato+plano:** `TbProfissional.IdContrato` é NOT NULL → o auto-cadastro
  cria um `TbContrato` (ligado a um `TbPlano`) antes/junto da criação do profissional.
- **Planos por tipo (extra):** não há campo ligando plano↔tipo; o filtro "planos de Médico/Nutri"
  será por **convenção de nome** (ex.: "Médico Total", "Médico Parcial", "Nutri Total", ...).
- **`IdCidade` é NOT NULL em `TbProfissional`:** o registro exige `tbCidade` populada → seed de cidades.
- **Regra "excluir só profissional sem pacientes":** já reforçada no banco por
  `DeleteBehavior.Restrict` em `tbMedico_Paciente → tbProfissional`; ainda assim checar em código
  com mensagem amigável (não depender só da exceção de FK).

### Preparação da Fase 5 (do review da Fase 4)

- **Ownership por `IdUser`:** Edit/Details do profissional filtram por
  `p.IdUser == _userManager.GetUserId(User)` — profissional só acessa o próprio registro.
- **CPF não editável:** garantir no **servidor** (reatribuir o CPF original antes de salvar, ou
  não bindar o campo). Esconder no HTML não basta.
- **Cidade:** na edição, derivar a string `Cidade` a partir de `IdCidade` (evitar dessincronizar).
- **Edição não junta login/senha:** só o Registro junta; a edição altera apenas dados do profissional.

### Preparação da Fase 6 (do review da Fase 5)

- **Controller separado** para gerentes (ex.: `ProfissionaisController`), sem mexer no `MeuPerfilController`
  (mais limpo + reforça anti-plágio). Usará `id` na URL (opera sobre terceiros).
- **Filtrar por tipo** usando o id do tipo via constante/mapa (1=Médico, 2=Nutricionista),
  validando no **servidor** que o alvo é compatível com a role do gerente (senão `Forbid()`),
  não confiar só no filtro do Index.
- **Sem Create** para gerentes.
- **Delete só sem pacientes:** checar `TbMedicoPaciente.AnyAsync(mp => mp.IdProfissional == id)`.
- **CPF editável** pelo gerente (incluir no bind do controller dos gerentes).
- Reaproveitar helper de cidades (extrair para serviço comum em vez de duplicar).
- **Limitação aceita:** sem índice único em `IdUser` (não alterar o design do `db_if`); a unicidade
  1 user ↔ 1 profissional é garantida pelo fluxo de registro (e-mail único → IdUser novo).

### Preparação da Fase 7 (do review da Fase 6)

- **`TbPacientesController` hoje é scaffold SEM `[Authorize]` e lista TODOS os pacientes** —
  precisa ser reescrito: exigir role `Medico,Nutricionista`; resolver `IdProfissional` via
  `IdUser` do logado; filtrar e validar ownership por `TbMedicoPaciente`; ao criar paciente,
  inserir a linha em `tbMedico_Paciente`.
- **Paciente é N:N** (pode ter médico e nutricionista): "excluir" pelo profissional deve, em
  geral, remover o **vínculo** (linha em `tbMedico_Paciente`); remover o `TbPaciente` afeta o
  vínculo de outro profissional. Checar dono sempre via `TbMedicoPaciente`, não via `TbPaciente`.
- Usar `FirstOrDefaultAsync` para `IdUser → IdProfissional` e tratar nulo (Forbid/NotFound).
- `InformacaoResumida` em `TbMedicoPaciente` é o campo por-profissional do paciente.

## 2. Entidades necessárias (mesmo design do `db_if`)

Mínimo para o trabalho:
- `TbProfissional` (tem `IdUser` ligando ao Identity, `Cpf`, `IdTipoProfissional`, `IdContrato`, `IdCidade`).
- `TbPaciente`, `TbCidade` (já existem no projeto).
- `TbMedicoPaciente` (vínculo profissional ↔ paciente — base da "lista dos meus pacientes").
- `TbContrato`, `TbPlano` (planos do profissional).
- `TbTipoProfissional` (Médico / Nutricionista) — se usado para distinguir tipo além das Roles.

> Geradas por EF Power Tools (se houver SQL Server) ou escritas à mão a partir do design conhecido.

---

## 3. Identity, Roles e Seed

**Roles:** `Medico`, `Nutricionista`, `GerenteMedico`, `GerenteNutricionista`, `GerenteGeral`.

**Seed no startup** (idempotente, roda 1x):
- Cria as 5 Roles se não existirem.
- Cria os **3 usuários Gerentes** e associa as Roles **direto no banco** (req. 2):
  `gerentemedico@...`, `gerentenutri@...`, `gerentegeral@...`.
- (Opcional) planos "Médico Total/Parcial" e equivalentes de Nutricionista.

---

## 4. Requisito 1 — Auto-cadastro do profissional

- Página de **Registro** (única que junta login + senha + dados do profissional) com escolha
  **clara** entre **Médico** e **Nutricionista** (ex.: dois botões/abas → `RegisterViewModel` com `TipoProfissional`).
- No POST: cria `IdentityUser`, adiciona à Role correspondente, cria `TbProfissional` com `IdUser`.
- Depois de cadastrado, o profissional só tem **Edit** e **Details** dos próprios dados:
  - **CPF não editável** no Edit (campo readonly + não incluído no update server-side).
  - Só acessa o **próprio** registro (ownership por `IdUser`).
- *(Extra +0,5):* planos "Médico Total/Parcial" só aparecem para Médico; equivalentes p/ Nutricionista.

---

## 5. Requisito 2 — Gerentes

- `[Authorize(Roles="GerenteMedico,GerenteNutricionista,GerenteGeral")]` no controller de profissionais.
- **Listagem filtrada por Role**:
  - GerenteMedico → só profissionais Médicos.
  - GerenteNutricionista → só Nutricionistas.
  - GerenteGeral → todos.
- Gerentes: **Edit, Details, Delete** — **sem Create** (action Create bloqueada para eles).
- Sem restrição de CPF (gerente edita CPF normalmente).
- **Delete só se o profissional não tiver pacientes** (checar `TbMedicoPaciente`).
- *(Extra +ponto):* deleção em cascata profissional + pacientes exclusivos.

---

## 6. Requisito 3 — Profissional gerencia seus pacientes (valor 8.0)

- Controller de pacientes com `[Authorize(Roles="Medico,Nutricionista")]`.
- CRUD completo: Create, Edit, Details, Delete.
- **Lista apenas dos pacientes do profissional logado**, via `TbMedicoPaciente`
  (filtrar por `IdProfissional` do usuário atual).
- Ao **criar** paciente: gravar também o vínculo em `TbMedicoPaciente`
  (análogo ao preenchimento do contrato do profissional).
- Ownership em Edit/Details/Delete: só pacientes vinculados ao profissional logado.

---

## 7. Segurança transversal

- Todas as actions sensíveis com `[Authorize(Roles=...)]` (não confiar em esconder links).
- `[ValidateAntiForgeryToken]` em todos os POST.
- Ownership checado no servidor em **toda** action que recebe `id`.
- `AccessDenied` configurado.

---

## 8. Tratamento de exceções e qualidade

- `try-catch (DbUpdateException)` em Create/Edit/Delete com mensagem amigável (`ModelState`/`ViewData`).
- `TryUpdateModelAsync` no Edit.
- Comentários com o nome do aluno acima de cada controller/action.
- Indentação e organização consistentes.

---

## 9. Apresentação (vídeo 8–12 min)

1. Mostrar o sistema rodando: registrar Médico e Nutricionista, login, CRUD de pacientes,
   visão de cada Gerente, tentativa de acesso negado.
2. Explicar o principal do código: Roles/seed, autorização no controller, ownership,
   `TryUpdateModelAsync`, tratamento de exceções, filtro de "meus pacientes".
3. Mostrar os extras, se implementados.

---

## 10. Ordem de execução proposta (fases)

1. **Infra**: connection strings + escolha SQLite/SQL Server + Identity + 2 contexts.
2. **Entidades** faltantes (Profissional, MedicoPaciente, Contrato, Plano, TipoProfissional).
3. **Seed** de Roles + 3 Gerentes.
4. **Registro** do profissional (Médico/Nutricionista) + criação do `TbProfissional`.
5. **Auto-serviço do profissional** (Edit/Details próprios, CPF travado, ownership).
6. **Área dos Gerentes** (listagem filtrada por Role, Edit/Details/Delete, regra de exclusão).
7. **CRUD de pacientes + meus pacientes** (vínculo `TbMedicoPaciente`).
8. **Tratamento de exceções + polish** + revisão de segurança.
9. **Extras** (planos por tipo, cascata) — se houver tempo.
10. **Roteiro do vídeo**.
