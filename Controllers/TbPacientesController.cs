using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Projeto1_Web2_IF_Lucas.Models;

// Lucas Wilman da Silva Crispim
namespace Projeto1_Web2_IF_Lucas.Controllers
{
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
            if (ModelState.IsValid)
            {
                _context.Add(tbPaciente);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
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
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        // Lucas Wilman da Silva Crispim
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdPaciente,Nome,Rg,Cpf,DataNascimento,NomeResponsavel,Sexo,Etnia,Endereco,Bairro,IdCidade,TelResidencial,TelComercial,TelCelular,Profissao,FlgAtleta,FlgGestante")] TbPaciente tbPaciente)
        {
            if (id != tbPaciente.IdPaciente)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tbPaciente);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TbPacienteExists(tbPaciente.IdPaciente))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["IdCidade"] = new SelectList(_context.TbCidade, "IdCidade", "Nome", tbPaciente.IdCidade);
            return View(tbPaciente);
        }

        // Lucas Wilman da Silva Crispim
        // GET: TbPacientes/Delete/5
        public async Task<IActionResult> Delete(int? id)
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
        // POST: TbPacientes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tbPaciente = await _context.TbPaciente.FindAsync(id);
            if (tbPaciente != null)
            {
                _context.TbPaciente.Remove(tbPaciente);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TbPacienteExists(int id)
        {
            return _context.TbPaciente.Any(e => e.IdPaciente == id);
        }
    }
}
