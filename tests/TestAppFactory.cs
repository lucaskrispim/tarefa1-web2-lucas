using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Projeto1_Web2_IF_Lucas.Tests
{
    // Sobe o app real (pipeline completo, Identity, migrations e seed) apontando para um
    // banco SQLite temporário e isolado por instância de factory. Assim cada classe de teste
    // tem o seu próprio banco já semeado (5 roles, 3 gerentes, tipos, cidades e planos).
    public class TestAppFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbPath =
            Path.Combine(Path.GetTempPath(), $"web2_test_{System.Guid.NewGuid():N}.sqlite");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, cfg) =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        $"Data Source={_dbPath};Foreign Keys=True"
                });
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing && File.Exists(_dbPath))
            {
                try { File.Delete(_dbPath); } catch { /* arquivo temporário */ }
            }
        }
    }
}
