using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Projeto1_Web2_IF_Lucas.Data;
using Projeto1_Web2_IF_Lucas.Models;

// Lucas Wilman da Silva Crispim
namespace Projeto1_Web2_IF_Lucas.Services
{
    public class PacienteService : IPacienteService
    {
        private readonly db_ifContext _context;

        public PacienteService(db_ifContext context) => _context = context;

        public async Task<SelectList> ObterCidadesAsync(int? selecionada)
            => await _context.CidadesSelectListAsync(selecionada);

        public async Task<int?> ObterIdProfissionalPorUsuarioAsync(string userId)
        {
            return await _context.TbProfissional
                .Where(p => p.IdUser == userId)
                .Select(p => (int?)p.IdProfissional)
                .FirstOrDefaultAsync();
        }

        public async Task<List<TbPaciente>> ListarDoProfissionalAsync(int idProfissional)
        {
            return await _context.TbMedicoPaciente
                .AsNoTracking()
                .Where(mp => mp.IdProfissional == idProfissional)
                .Include(mp => mp.IdPacienteNavigation)
                    .ThenInclude(p => p.IdCidadeNavigation)
                .Select(mp => mp.IdPacienteNavigation)
                .OrderBy(p => p.Nome)
                .ToListAsync();
        }

        public async Task<TbPaciente?> ObterDoProfissionalAsync(int idPaciente, int idProfissional, bool comNavegacoes)
        {
            IQueryable<TbPaciente> query = _context.TbPaciente
                .Where(p => p.IdPaciente == idPaciente
                    && p.TbMedicoPaciente.Any(mp => mp.IdProfissional == idProfissional));

            if (comNavegacoes)
            {
                query = query.Include(p => p.IdCidadeNavigation);
            }

            return await query.FirstOrDefaultAsync();
        }

        public async Task<string?> ObterInformacaoResumidaAsync(int idPaciente, int idProfissional)
        {
            return await _context.TbMedicoPaciente
                .Where(mp => mp.IdPaciente == idPaciente && mp.IdProfissional == idProfissional)
                .Select(mp => mp.InformacaoResumida)
                .FirstOrDefaultAsync();
        }

        public async Task CriarAsync(TbPaciente paciente, int idProfissional, string? informacaoResumida)
        {
            // O vínculo referencia o paciente por navegação: um único SaveChanges insere
            // o paciente e a linha em tbMedico_Paciente atomicamente.
            var vinculo = new TbMedicoPaciente
            {
                IdProfissional = idProfissional,
                IdPacienteNavigation = paciente,
                InformacaoResumida = informacaoResumida
            };
            _context.TbMedicoPaciente.Add(vinculo);
            await _context.SaveChangesAsync();
        }

        public async Task SalvarEdicaoAsync(TbPaciente paciente, int idProfissional, string? informacaoResumida)
        {
            // O 'paciente' já vem rastreado e alterado pelo controller (mesmo DbContext scoped);
            // aqui atualizamos a anotação do vínculo deste profissional e persistimos tudo junto.
            var vinculo = await _context.TbMedicoPaciente
                .FirstOrDefaultAsync(mp => mp.IdPaciente == paciente.IdPaciente && mp.IdProfissional == idProfissional);
            if (vinculo != null)
            {
                vinculo.InformacaoResumida = informacaoResumida;
            }
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExcluirDoProfissionalAsync(int idPaciente, int idProfissional)
        {
            var vinculo = await _context.TbMedicoPaciente
                .FirstOrDefaultAsync(mp => mp.IdPaciente == idPaciente && mp.IdProfissional == idProfissional);
            if (vinculo == null)
            {
                return false; // não é paciente deste profissional (ou já removido)
            }

            using var tx = await _context.Database.BeginTransactionAsync();

            _context.TbMedicoPaciente.Remove(vinculo);
            await _context.SaveChangesAsync();

            // Se ninguém mais estiver vinculado, remove o paciente órfão.
            var aindaVinculado = await _context.TbMedicoPaciente.AnyAsync(mp => mp.IdPaciente == idPaciente);
            if (!aindaVinculado)
            {
                var paciente = await _context.TbPaciente.FirstOrDefaultAsync(p => p.IdPaciente == idPaciente);
                if (paciente != null)
                {
                    _context.TbPaciente.Remove(paciente);
                    await _context.SaveChangesAsync();
                }
            }

            await tx.CommitAsync();
            return true;
        }
    }
}
