using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using SistemaEleitoral.Domain.Entities;

namespace SistemaEleitoral.Domain.Interfaces.Repositories
{
    public interface IBaseRepository<T> where T : class
    {
        Task<T> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> expression);
        Task<T> AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(T entity);
        Task<bool> ExistsAsync(Expression<Func<T, bool>> expression);
    }

    public interface IResultadoRepository : IBaseRepository<ResultadoApuracao>
    {
        Task<ResultadoApuracao> ObterResultadoPorCalendarioAsync(int calendarioId, int? ufId);
        Task<List<VotoChapa>> ObterVotosPorChapaAsync(int resultadoId);
        Task UpdateVotoChapaAsync(VotoChapa votoChapa);
        Task<bool> VerificarRecontagensPendentesAsync(int resultadoId);
        Task<SolicitacaoRecontagem> ObterSolicitacaoRecontagemAsync(int solicitacaoId);
        Task AddSolicitacaoRecontagemAsync(SolicitacaoRecontagem solicitacao);
        Task UpdateSolicitacaoRecontagemAsync(SolicitacaoRecontagem solicitacao);
        Task AddResultadoRecontagemAsync(ResultadoRecontagem recontagem);
        Task AddImpugnacaoResultadoAsync(ImpugnacaoResultado impugnacao);
        Task<List<ResultadoApuracao>> ObterUltimosResultadosAsync(int? ufId, int quantidade);
    }

    public interface IVotoRepository : IBaseRepository<Voto>
    {
        Task<List<Voto>> ObterVotosPorSessaoAsync(int calendarioId, int ufId);
    }

    public interface IChapaRepository : IBaseRepository<ChapaEleicao>
    {
    }

    public interface ICalendarioRepository : IBaseRepository<Calendario>
    {
    }

    public interface IEmailLogRepository : IBaseRepository<EmailLog>
    {
        Task<List<EmailLog>> ObterPendentesAsync(int limite);
        Task<List<EmailLog>> ObterPorStatusAsync(string status);
    }
}