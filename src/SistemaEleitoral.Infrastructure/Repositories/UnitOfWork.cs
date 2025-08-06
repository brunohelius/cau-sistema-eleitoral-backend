using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SistemaEleitoral.Domain.Entities;
using SistemaEleitoral.Domain.Interfaces;
using SistemaEleitoral.Infrastructure.Data;

namespace SistemaEleitoral.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILoggerFactory _loggerFactory;
        private IDbContextTransaction? _transaction;

        // Core Electoral System Repositories
        private ICalendarioRepository? _calendarios;
        private IEleicaoRepository? _eleicoes;
        private IChapaEleicaoRepository? _chapas;
        private IComissaoEleitoralRepository? _comissoes;
        private IProfissionalRepository? _profissionais;
        private IMembroChapaRepository? _membrosChapa;

        // Judicial System Repositories
        private IJulgamentoDenunciaRepository? _julgamentosDenuncia;
        private IJulgamentoImpugnacaoRepository? _julgamentosImpugnacao;
        private IRecursoDenunciaRepository? _recursosDenuncia;
        private IDefesaDenunciaRepository? _defesasDenuncia;

        // Document Management Repositories
        private IArquivoRepository? _arquivos;
        private IDocumentoEleicaoRepository? _documentosEleicao;
        private ITemplateDocumentoRepository? _templatesDocumento;

        // Support/Lookup Repositories
        private IUfRepository? _ufs;
        private ITipoProcessoRepository? _tiposProcesso;
        private ISituacaoCalendarioRepository? _situacoesCalendario;

        // Historical/Audit Repositories
        private IAuditoriaRepository? _auditorias;
        private IHistoricoCalendarioRepository? _historicosCalendario;
        private IHistoricoChapaRepository? _historicosChapa;

        // Email/Notification Repositories
        private IEmailRepository? _emails;
        private INotificacaoRepository? _notificacoes;
        private ITemplateEmailRepository? _templatesEmail;
        private IJobEmailRepository? _jobsEmail;

        // Specialized Repositories
        private IIntegracaoSiccauRepository? _integracoesSiccau;
        private IRelatorioEleitoralRepository? _relatoriosEleitorais;
        private IConfiguracaoSistemaRepository? _configuracoesSistema;

        public UnitOfWork(ApplicationDbContext context, IMemoryCache cache, ILoggerFactory loggerFactory)
        {
            _context = context;
            _cache = cache;
            _loggerFactory = loggerFactory;
        }

        #region Core Electoral System Properties

        public ICalendarioRepository Calendarios =>
            _calendarios ??= new CalendarioRepository(_context, _cache, _loggerFactory.CreateLogger<CalendarioRepository>());

        public IEleicaoRepository Eleicoes =>
            _eleicoes ??= new EleicaoRepository(_context, _cache, _loggerFactory.CreateLogger<EleicaoRepository>());

        public IChapaEleicaoRepository Chapas =>
            _chapas ??= new ChapaEleicaoRepository(_context, _cache, _loggerFactory.CreateLogger<ChapaEleicaoRepository>());

        public IComissaoEleitoralRepository Comissoes =>
            _comissoes ??= new ComissaoEleitoralRepository(_context, _cache, _loggerFactory.CreateLogger<ComissaoEleitoralRepository>());

        public IProfissionalRepository Profissionais =>
            _profissionais ??= new ProfissionalRepository(_context, _cache, _loggerFactory.CreateLogger<ProfissionalRepository>());

        public IMembroChapaRepository MembrosChapa =>
            _membrosChapa ??= new MembroChapaRepository(_context, _cache, _loggerFactory.CreateLogger<MembroChapaRepository>());

        #endregion

        #region Judicial System Properties

        public IJulgamentoDenunciaRepository JulgamentosDenuncia =>
            _julgamentosDenuncia ??= new JulgamentoDenunciaRepository(_context, _cache, _loggerFactory.CreateLogger<JulgamentoDenunciaRepository>());

        public IJulgamentoImpugnacaoRepository JulgamentosImpugnacao =>
            _julgamentosImpugnacao ??= new JulgamentoImpugnacaoRepository(_context, _cache, _loggerFactory.CreateLogger<JulgamentoImpugnacaoRepository>());

        public IRecursoDenunciaRepository RecursosDenuncia =>
            _recursosDenuncia ??= new RecursoDenunciaRepository(_context, _cache, _loggerFactory.CreateLogger<RecursoDenunciaRepository>());

        public IDefesaDenunciaRepository DefesasDenuncia =>
            _defesasDenuncia ??= new DefesaDenunciaRepository(_context, _cache, _loggerFactory.CreateLogger<DefesaDenunciaRepository>());

        #endregion

        #region Document Management Properties

        public IArquivoRepository Arquivos =>
            _arquivos ??= new ArquivoRepository(_context, _cache, _loggerFactory.CreateLogger<ArquivoRepository>(), null);

        public IDocumentoEleicaoRepository DocumentosEleicao =>
            _documentosEleicao ??= new DocumentoEleicaoRepository(_context, _cache, _loggerFactory.CreateLogger<DocumentoEleicaoRepository>());

        public ITemplateDocumentoRepository TemplatesDocumento =>
            _templatesDocumento ??= new TemplateDocumentoRepository(_context, _cache, _loggerFactory.CreateLogger<TemplateDocumentoRepository>());

        #endregion

        #region Support/Lookup Properties

        public IUfRepository Ufs =>
            _ufs ??= new UfRepository(_context, _cache, _loggerFactory.CreateLogger<UfRepository>());

        public ITipoProcessoRepository TiposProcesso =>
            _tiposProcesso ??= new TipoProcessoRepository(_context, _cache, _loggerFactory.CreateLogger<TipoProcessoRepository>());

        public ISituacaoCalendarioRepository SituacoesCalendario =>
            _situacoesCalendario ??= new SituacaoCalendarioRepository(_context, _cache, _loggerFactory.CreateLogger<SituacaoCalendarioRepository>());

        #endregion

        #region Historical/Audit Properties

        public IAuditoriaRepository Auditorias =>
            _auditorias ??= new AuditoriaRepository(_context, _cache, _loggerFactory.CreateLogger<AuditoriaRepository>());

        public IHistoricoCalendarioRepository HistoricosCalendario =>
            _historicosCalendario ??= new HistoricoCalendarioRepository(_context, _cache, _loggerFactory.CreateLogger<HistoricoCalendarioRepository>());

        public IHistoricoChapaRepository HistoricosChapa =>
            _historicosChapa ??= new HistoricoChapaRepository(_context, _cache, _loggerFactory.CreateLogger<HistoricoChapaRepository>());

        #endregion

        #region Email/Notification Properties

        public IEmailRepository Emails =>
            _emails ??= new EmailRepository(_context, _cache, _loggerFactory.CreateLogger<EmailRepository>());

        public INotificacaoRepository Notificacoes =>
            _notificacoes ??= new NotificacaoRepository(_context, _cache, _loggerFactory.CreateLogger<NotificacaoRepository>());

        public ITemplateEmailRepository TemplatesEmail =>
            _templatesEmail ??= new TemplateEmailRepository(_context, _cache, _loggerFactory.CreateLogger<TemplateEmailRepository>());

        public IJobEmailRepository JobsEmail =>
            _jobsEmail ??= new JobEmailRepository(_context, _cache, _loggerFactory.CreateLogger<JobEmailRepository>());

        #endregion

        #region Specialized Properties

        public IIntegracaoSiccauRepository IntegracoesSiccau =>
            _integracoesSiccau ??= new IntegracaoSiccauRepository(_context, _cache, _loggerFactory.CreateLogger<IntegracaoSiccauRepository>());

        public IRelatorioEleitoralRepository RelatoriosEleitorais =>
            _relatoriosEleitorais ??= new RelatorioEleitoralRepository(_context, _cache, _loggerFactory.CreateLogger<RelatorioEleitoralRepository>());

        public IConfiguracaoSistemaRepository ConfiguracoesSistema =>
            _configuracoesSistema ??= new ConfiguracaoSistemaRepository(_context, _cache, _loggerFactory.CreateLogger<ConfiguracaoSistemaRepository>());

        #endregion

        #region Transaction Management

        public async Task<int> SaveChangesAsync()
        {
            try
            {
                return await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Log the error and re-throw
                var logger = _loggerFactory.CreateLogger<UnitOfWork>();
                logger.LogError(ex, "Error occurred while saving changes to database");
                throw;
            }
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            try
            {
                return await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                // Log the error and re-throw
                var logger = _loggerFactory.CreateLogger<UnitOfWork>();
                logger.LogError(ex, "Error occurred while saving changes to database");
                throw;
            }
        }

        public async Task BeginTransactionAsync()
        {
            if (_transaction == null)
            {
                _transaction = await _context.Database.BeginTransactionAsync();
            }
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                try
                {
                    await _context.SaveChangesAsync();
                    await _transaction.CommitAsync();
                }
                finally
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                try
                {
                    await _transaction.RollbackAsync();
                }
                finally
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }

        #endregion

        #region Repository Creation

        public IRepository<T> Repository<T>() where T : class
        {
            return new GenericRepository<T>(_context, _cache, _loggerFactory.CreateLogger<GenericRepository<T>>());
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _transaction?.Dispose();
            _context?.Dispose();
        }

        #endregion
    }

    // Generic repository for dynamic entity access
    public class GenericRepository<T> : BaseRepository<T> where T : BaseEntity
    {
        public GenericRepository(ApplicationDbContext context, IMemoryCache cache, ILogger<GenericRepository<T>> logger)
            : base(context, cache, logger)
        {
        }
    }

    // Individual repository implementations need to be referenced
    // These classes should exist in their respective batch files or individual files
    public class CalendarioRepository : BaseRepository<Calendario>, ICalendarioRepository
    {
        public CalendarioRepository(ApplicationDbContext context, IMemoryCache cache, ILogger<CalendarioRepository> logger)
            : base(context, cache, logger) { }
    }

    public class ChapaEleicaoRepository : BaseRepository<ChapaEleicao>, IChapaEleicaoRepository
    {
        public ChapaEleicaoRepository(ApplicationDbContext context, IMemoryCache cache, ILogger<ChapaEleicaoRepository> logger)
            : base(context, cache, logger) { }
    }
}