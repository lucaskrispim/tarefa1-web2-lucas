using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Projeto1_Web2_IF_Lucas.Data;
using Projeto1_Web2_IF_Lucas.Models;

// Lucas Wilman da Silva Crispim
namespace Projeto1_Web2_IF_Lucas.Controllers
{
    // LEGADO (Tarefa de acompanhamento 2 - Database First). Não faz parte do fluxo do Trabalho
    // Final; restrito a GerenteGeral para não expor pacientes sem filtro. O CRUD do final é MeusPacientes.
    [Authorize(Roles = DbInitializer.RoleGerenteGeral)]
    public class TbPacientesController : Controller
    {
        private readonly db_ifContext _context;

        public TbPacientesController(db_ifContext context)
        {
            _context = context;
        }

        // Lucas Wilman da Silva Crispim
        // GET: TbPacientes
        public async Task<IActionResult> Index()
        {
            var db_ifContext = _context.TbPaciente.Include(t => t.IdCidadeNavigation);
            return View(await db_ifContext.ToListAsync());
        }

        // Lucas Wilman da Silva Crispim
        // GET: TbPacientes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tbPaciente = await _context.TbPaciente
                .Include(t => t.IdCidadeNavigation)
                .FirstOrDefaultAsync(m => m.IdPaciente == id);
            if (tbPaciente == null)
            {
                return NotFound();
            }

            return View(tbPaciente);
        }

        // Lucas Wilman da Silva Crispim
        // GET: TbPacientes/Create
        public IActionResult Create()
        {
            ViewData["IdCidade"] = new SelectList(_context.TbCidade, "IdCidade", "Nome");
            return View();
        }

        // POST: TbPacientes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        // Lucas Wilman da Silva Crispim
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdPaciente,Nome,Rg,Cpf,DataNascimento,NomeResponsavel,Sexo,Etnia,Endereco,Bairro,IdCidade,TelResidencial,TelComercial,TelCelular,Profissao,FlgAtleta,FlgGestante")] TbPaciente tbPaciente)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    _context.Add(tbPaciente);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (DbUpdateException /* ex */)
            {
                // Registre o erro (uncomment ex variable name and write a log).
                ModelState.AddModelError("", "Não foi possível salvar as alterações. " +
                    "Tente novamente e, se o problema persistir, " +
                    "contate o administrador do sistema.");
            }
            ViewData["IdCidade"] = new SelectList(_context.TbCidade, "IdCidade", "Nome", tbPaciente.IdCidade);
            return View(tbPaciente);
        }

        // Lucas Wilman da Silva Crispim
        // GET: TbPacientes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tbPaciente = await _context.TbPaciente.FindAsync(id);
            if (tbPaciente == null)
            {
                return NotFound();
            }
            ViewData["IdCidade"] = new SelectList(_context.TbCidade, "IdCidade", "Nome", tbPaciente.IdCidade);
            return View(tbPaciente);
        }

        // POST: TbPacientes/Edit/5
        // Conforme o tutorial da Microsoft (CRUD - EF Core), usa-se TryUpdateModelAsync
        // em vez de [Bind], evitando overposting e atualizando apenas as propriedades informadas.
        // Lucas Wilman da Silva Crispim
        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tbPacienteToUpdate = await _context.TbPaciente
                .FirstOrDefaultAsync(p => p.IdPaciente == id);

            if (tbPacienteToUpdate == null)
            {
                return NotFound();
            }

            if (await TryUpdateModelAsync<TbPaciente>(
                tbPacienteToUpdate,
                "",
                p => p.Nome, p => p.Rg, p => p.Cpf, p => p.DataNascimento,
                p => p.NomeResponsavel, p => p.Sexo, p => p.Etnia, p => p.Endereco,
                p => p.Bairro, p => p.IdCidade, p => p.TelResidencial, p => p.TelComercial,
                p => p.TelCelular, p => p.Profissao, p => p.FlgAtleta, p => p.FlgGestante))
            {
                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException /* ex */)
                {
                    // Registre o erro (uncomment ex variable name and write a log).
                    ModelState.AddModelError("", "Não foi possível salvar as alterações. " +
                        "Tente novamente e, se o problema persistir, " +
                        "contate o administrador do sistema.");
                }
            }
            ViewData["IdCidade"] = new SelectList(_context.TbCidade, "IdCidade", "Nome", tbPacienteToUpdate.IdCidade);
            return View(tbPacienteToUpdate);
        }

        // Lucas Wilman da Silva Crispim
        // GET: TbPacientes/Delete/5
        public async Task<IActionResult> Delete(int? id, bool? saveChangesError = false)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tbPaciente = await _context.TbPaciente
                .Include(t => t.IdCidadeNavigation)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.IdPaciente == id);
            if (tbPaciente == null)
            {
                return NotFound();
            }

            if (saveChangesError.GetValueOrDefault())
            {
                ViewData["ErrorMessage"] =
                    "Não foi possível excluir. Tente novamente e, se o problema persistir, " +
                    "contate o administrador do sistema.";
            }

            return View(tbPaciente);
        }

        // Lucas Wilman da Silva Crispim
        // POST: TbPacientes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tbPaciente = await _context.TbPaciente.FindAsync(id);
            if (tbPaciente == null)
            {
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.TbPaciente.Remove(tbPaciente);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException /* ex */)
            {
                // Registre o erro (uncomment ex variable name and write a log).
                return RedirectToAction(nameof(Delete), new { id = id, saveChangesError = true });
            }
        }

        private bool TbPacienteExists(int id)
        {
            return _context.TbPaciente.Any(e => e.IdPaciente == id);
        }
    }
}
