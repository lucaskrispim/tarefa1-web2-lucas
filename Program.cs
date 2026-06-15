using Microsoft.EntityFrameworkCore;
using Projeto1_Web2_IF_Lucas.Data;
using Projeto1_Web2_IF_Lucas.Models;

// Lucas Wilman da Silva Crispim
var builder = WebApplication.CreateBuilder(args);

// =========================================================================
// Tarefa de acompanhamento 2 - Database First (conforme o vídeo)
// O model TbPaciente e o db_ifContext foram obtidos por engenharia reversa
// do banco "db_if" (SQL Server), usando o EF Core Power Tools.
// =========================================================================

// Connection string do banco db_if (SQL Server) -> usada pelo db_ifContext.
var dbIfConnection = builder.Configuration.GetConnectionString("DbIfConnection")
    ?? throw new InvalidOperationException("Connection string 'DbIfConnection' not found.");

builder.Services.AddDbContext<db_ifContext>(options =>
    options.UseSqlServer(dbIfConnection));

// -------------------------------------------------------------------------
// PARA RODAR LOCAL EM SQLite (caso você não tenha o SQL Server / banco db_if):
// 1) Comente as 2 linhas acima (AddDbContext<db_ifContext> com UseSqlServer).
// 2) Descomente o bloco abaixo:
//
// builder.Services.AddDbContext<db_ifContext>(options =>
//     options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
// -------------------------------------------------------------------------

// Contexto da Tarefa 1 (model Paciente, SQLite) - mantido para histórico.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=db_if.sqlite";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Aplica as migrations / cria o banco SQLite do contexto da Tarefa 1.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
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

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
