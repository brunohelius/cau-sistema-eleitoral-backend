using SistemaEleitoral.Domain.Entities;

namespace SistemaEleitoral.Domain.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        // Repositories - Core Electoral System
        ICalendarioRepository Calendarios { get; }
        IEleicaoRepository Eleicoes { get; }
        IChapaEleicaoRepository Chapas { get; }
        IComissaoEleitoralRepository Comissoes { get; }
        IProfissionalRepository Profissionais { get; }
        IMembroChapaRepository MembrosChapa { get; }

        // Transaction management
        Task<int> SaveChangesAsync();
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();

        // Repository creation for dynamic access
        IRepository<T> Repository<T>() where T : class;
    }

    // Individual repository interfaces (declared here for UnitOfWork contract)
    public interface ICalendarioRepository : IRepository<Calendario> { }
    public interface IEleicaoRepository : IRepository<Eleicao> { }
    public interface IChapaEleicaoRepository : IRepository<ChapaEleicao> { }
    public interface IComissaoEleitoralRepository : IRepository<ComissaoEleitoral> { }
    public interface IProfissionalRepository : IRepository<Profissional> { }
    public interface IMembroChapaRepository : IRepository<MembroChapa> { }
    
    // Other interfaces are declared in their respective batch files
}