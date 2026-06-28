using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Projeto1_Web2_IF_Lucas.Models;

// Lucas Wilman da Silva Crispim
namespace Projeto1_Web2_IF_Lucas.Data
{
    // Centraliza a construção de SelectLists reutilizadas pelos controllers (evita duplicação).
    public static class SelectListExtensions
    {
        public static async Task<SelectList> CidadesSelectListAsync(this db_ifContext context, int? selecionada = null)
        {
            return new SelectList(
                await context.TbCidade.AsNoTracking().OrderBy(c => c.Nome).ToListAsync(),
                nameof(TbCidade.IdCidade), nameof(TbCidade.Nome), selecionada);
        }
    }
}
