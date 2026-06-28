using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Projeto1_Web2_IF_Lucas.Data;
using Projeto1_Web2_IF_Lucas.Models;
using Projeto1_Web2_IF_Lucas.Services;

// Lucas Wilman da Silva Crispim
var builder = WebApplication.CreateBuilder(args);

// =========================================================================
// Trabalho Final - Programação Web II
// Desenvolvimento em SQLite (db_if.sqlite). O design do banco db_if é mantido;
// apenas o provedor muda para rodar localmente sem SQL Server.
// =========================================================================

// Connection string do SQLite (dev). Para entregar em SQL Server, basta trocar o
// provedor abaixo para UseSqlServer e usar a connection string "DbIfConnection".
var sqliteConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=db_if.sqlite";

// -------------------------------------------------------------------------
// Contexto do Identity (tabelas AspNet*) -> base SQLite.
// -------------------------------------------------------------------------
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(sqliteConnection));

// -------------------------------------------------------------------------
// Contexto de negócio db_if (TbPaciente, TbCidade, ...) -> mesma base SQLite.
// Usa uma tabela de histórico de migrations própria para coexistir com o
// ApplicationDbContext no mesmo arquivo .sqlite.
// -------------------------------------------------------------------------
builder.Services.AddDbContext<db_ifContext>(options =>
    options.UseSqlite(sqliteConnection,
        x => x.MigrationsHistoryTable("__EFMigrationsHistory_DbIf")));

// -------------------------------------------------------------------------
// ASP.NET Core Identity + Roles.
// AddDefaultIdentity já fornece as páginas de Login/Registro (Identity.UI).
// AddRoles habilita autorização baseada em papéis (Médico, Nutricionista, Gerentes).
// -------------------------------------------------------------------------
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Caminhos do cookie de autenticação explícitos (login e acesso negado).
// Quando um usuário autenticado sem a Role tentar acessar uma action protegida,
// será levado para a página de AccessDenied (autorização por Role é requisito).
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

// Camada de serviço (regras de negócio fora dos controllers).
builder.Services.AddScoped<IPacienteService, PacienteService>();
builder.Services.AddScoped<IProfissionalService, ProfissionalService>();
builder.Services.AddScoped<IRegistroService, RegistroService>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); // necessário para as páginas do Identity

var app = builder.Build();

// Aplica as migrations / cria o banco SQLite dos dois contextos e roda o seed no startup.
// Falha aqui é fail-fast (não sobe com banco inconsistente), mas é registrada no log.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        services.GetRequiredService<ApplicationDbContext>().Database.Migrate();
        services.GetRequiredService<db_ifContext>().Database.Migrate();
        await DbInitializer.SeedAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Erro ao migrar/semear o banco de dados no startup.");
        throw;
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Desabilita a página de registro padrão do Identity: o cadastro de profissional é feito
// apenas em /Conta/Registrar (com escolha de Médico/Nutricionista). Redireciona quem tentar.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/Identity/Account/Register"))
    {
        context.Response.Redirect("/Conta/Registrar");
        return;
    }
    await next();
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();

// Expõe a classe Program (gerada por top-level statements) para o projeto de testes
// de integração (WebApplicationFactory<Program>).
public partial class Program { }
