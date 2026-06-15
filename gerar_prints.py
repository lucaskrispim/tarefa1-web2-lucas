#!/usr/bin/env python3
# Lucas Wilman da Silva Crispim
# Gera os 7 prints da Tarefa de acompanhamento 2 (TbPaciente / Database First) como PNG.
import os
from pygments import highlight
from pygments.lexers import CSharpLexer, JavascriptLexer
from pygments.formatters import ImageFormatter

BASE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(BASE, "prints_tarefa2")
os.makedirs(OUT, exist_ok=True)

HDR = "// Lucas Wilman da Silva Crispim - Tarefa de acompanhamento 2\n"

create = '''// TbPacientesController.cs

// Lucas Wilman da Silva Crispim
// POST: TbPacientes/Create
// To protect from overposting attacks, enable the specific properties you want to bind to.
// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
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
'''

details = '''// TbPacientesController.cs

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
'''

edit = '''// TbPacientesController.cs

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
'''

editpost = '''// TbPacientesController.cs

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
'''

delete = '''// TbPacientesController.cs

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
'''

deleteconfirmed = '''// TbPacientesController.cs

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
'''

with open(os.path.join(BASE,
          "wwwroot/lib/jquery-validation/dist/jquery.validate.pt-br.js"), encoding="utf-8") as f:
    jsfile = f.read()

cs_items = [
    ("1_Create_com_Bind", create),
    ("2_Details", details),
    ("3_Edit", edit),
    ("4_EditPost", editpost),
    ("5_Delete", delete),
    ("6_DeleteConfirmed", deleteconfirmed),
]

def fmt():
    return ImageFormatter(font_name="DejaVu Sans Mono", font_size=22,
                          line_numbers=True, style="monokai",
                          line_pad=6, image_pad=20)

for name, code in cs_items:
    img = highlight(HDR + code, CSharpLexer(), fmt())
    with open(os.path.join(OUT, f"{name}.png"), "wb") as o:
        o.write(img)
    print("ok:", name)

img = highlight(jsfile, JavascriptLexer(), fmt())
with open(os.path.join(OUT, "7_jquery.validate.pt-br.png"), "wb") as o:
    o.write(img)
print("ok: 7_jquery.validate.pt-br")
print("Saida:", OUT)
