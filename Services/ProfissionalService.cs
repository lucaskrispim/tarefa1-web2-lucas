using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Projeto1_Web2_IF_Lucas.Data;
using Projeto1_Web2_IF_Lucas.Models;

// Lucas Wilman da Silva Crispim
namespace Projeto1_Web2_IF_Lucas.Services
{
    public class ProfissionalService : IProfissionalService
    {
        private readonly db_ifContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<ProfissionalService> _logger;

        public ProfissionalService(
            db_ifContext context,
            UserManager<IdentityUser> userManager,
            ILogger<ProfissionalService> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<SelectList> ObterCidadesAsync(int? selecionada)
            => await _context.CidadesSelectListAsync(selecionada);

        public async Task<TbProfissional?> ObterPorUsuarioAsync(string userId, bool comNavegacoes)
        {
            IQueryable<TbProfissional> query = _context.TbProfissional;
            if (comNavegacoes)
            {
                query = query
                    .Include(p => p.IdCidadeNavigation)
                    .Include(p => p.IdTipoProfissionalNavigation)
                    .Include(p => p.IdContratoNavigation)
                        .ThenInclude(c => c.IdPlanoNavigation);
            }
            return await query.FirstOrDefaultAsync(p => p.IdUser == userId);
        }

        public async Task<List<TbProfissional>> ListarPorEscopoAsync(int? tipoPermitido)
        {
            IQueryable<TbProfissional> query = _context.TbProfissional
                .AsNoTracking()
                .Include(p => p.IdTipoProfissionalNavigation)
                .Include(p => p.IdCidadeNavigation);

            if (tipoPermitido != null)
            {
                query = query.Where(p => p.IdTipoProfissional == tipoPermitido);
            }

            return await query.OrderBy(p => p.Nome).ToListAsync();
        }

        public async Task<TbProfissional?> ObterAsync(int id, bool comNavegacoes)
        {
            IQueryable<TbProfissional> query = _context.TbProfissional;
            if (comNavegacoes)
            {
                query = query
                    .Include(p => p.IdTipoProfissionalNavigation)
                    .Include(p => p.IdCidadeNavigation)
                    .Include(p => p.IdContratoNavigation)
                        .ThenInclude(c => c.IdPlanoNavigation);
            }
            return await query.FirstOrDefaultAsync(p => p.IdProfissional == id);
        }

        public async Task<List<PacienteVinculoInfo>> ObterPacientesVinculadosAsync(int idProfissional)
        {
            var vinculos = await _context.TbMedicoPaciente
                .AsNoTracking()
                .Where(mp => mp.IdProfissional == idProfissional)
                .Include(mp => mp.IdPacienteNavigation)
                .ToListAsync();

            var ids = vinculos.Select(v => v.IdPaciente).ToList();
            var contagem = await _context.TbMedicoPaciente
                .Where(mp => ids.Contains(mp.IdPaciente))
                .GroupBy(mp => mp.IdPaciente)
                .Select(g => new { Id = g.Key, Qtd = g.Count() })
                .ToDictionaryAsync(x => x.Id, x => x.Qtd);

            return vinculos
                .Select(v => new PacienteVinculoInfo(
                    v.IdPaciente, v.IdPacienteNavigation.Nome, contagem[v.IdPaciente] == 1))
                .ToList();
        }

        public async Task SalvarComCidadeAsync(TbProfissional profissional)
        {
            var cidade = await _context.TbCidade
                .FirstOrDefaultAsync(c => c.IdCidade == profissional.IdCidade);
            profissional.Cidade = cidade?.Nome;
            await _context.SaveChangesAsync();
        }

        public async Task<ExclusaoResultado> ExcluirAsync(int id, bool cascata)
        {
            var profissional = await _context.TbProfissional
                .FirstOrDefaultAsync(p => p.IdProfissional == id);
            if (profissional == null)
            {
                return ExclusaoResultado.De(StatusExclusao.NaoEncontrado);
            }

            var vinculos = await _context.TbMedicoPaciente
                .Where(mp => mp.IdProfissional == id)
                .ToListAsync();

            // Sem cascata, vale a regra base: só exclui profissional sem pacientes.
            if (vinculos.Count > 0 && !cascata)
            {
                return ExclusaoResultado.De(StatusExclusao.BloqueadoComPacientes);
            }

            var contrato = await _context.TbContrato
                .FirstOrDefaultAsync(c => c.IdContrato == profissional.IdContrato);
            var idUser = profissional.IdUser;
            var idsPacientes = vinculos.Select(v => v.IdPaciente).Distinct().ToList();
            var exclusivosRemovidos = 0;

            using (var tx = await _context.Database.BeginTransactionAsync())
            {
                // 1) Remove os vínculos deste profissional (libera a FK Restrict).
                if (vinculos.Count > 0)
                {
                    _context.TbMedicoPaciente.RemoveRange(vinculos);
                    await _context.SaveChangesAsync();
                }

                // 2) Cascata: remove os pacientes que ficaram sem nenhum profissional.
                if (cascata && idsPacientes.Count > 0)
                {
                    var orfaos = await _context.TbPaciente
                        .Where(p => idsPacientes.Contains(p.IdPaciente) && !p.TbMedicoPaciente.Any())
                        .ToListAsync();
                    if (orfaos.Count > 0)
                    {
                        _context.TbPaciente.RemoveRange(orfaos);
                        await _context.SaveChangesAsync();
                        exclusivosRemovidos = orfaos.Count;
                    }
                }

                // 3) Remove o profissional e o seu contrato.
                _context.TbProfissional.Remove(profissional);
                await _context.SaveChangesAsync();
                if (contrato != null)
                {
                    _context.TbContrato.Remove(contrato);
                    await _context.SaveChangesAsync();
                }

                await tx.CommitAsync();
            }

            // 4) Remove a conta de login (contexto separado do Identity).
            var user = await _userManager.FindByIdAsync(idUser);
            if (user != null)
            {
                var del = await _userManager.DeleteAsync(user);
                if (!del.Succeeded)
                {
                    _logger.LogError("Profissional {Id} excluído, mas falhou remover o usuário {UserId}: {Erros}",
                        id, idUser, string.Join("; ", del.Errors.Select(e => e.Description)));
                    return ExclusaoResultado.De(StatusExclusao.SucessoComAvisoLogin, exclusivosRemovidos);
                }
            }

            return ExclusaoResultado.De(StatusExclusao.Sucesso, exclusivosRemovidos);
        }
    }
}
