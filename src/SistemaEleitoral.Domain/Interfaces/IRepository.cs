using System.Linq.Expressions;

namespace SistemaEleitoral.Domain.Interfaces
{
    public interface IRepository<T> where T : class
    {
        // Basic CRUD operations
        Task<T?> GetByIdAsync(int id);
        Task<T?> GetByIdAsync(int id, params Expression<Func<T, object>>[] includes);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[] includes);
        Task<T> AddAsync(T entity);
        Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);
        Task<T> UpdateAsync(T entity);
        Task<IEnumerable<T>> UpdateRangeAsync(IEnumerable<T> entities);
        Task DeleteAsync(int id);
        Task DeleteAsync(T entity);
        Task DeleteRangeAsync(IEnumerable<T> entities);

        // Query operations
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
        Task<int> CountAsync();
        Task<int> CountAsync(Expression<Func<T, bool>> predicate);

        // Paging operations
        Task<IEnumerable<T>> GetPagedAsync(int pageNumber, int pageSize);
        Task<IEnumerable<T>> GetPagedAsync(int pageNumber, int pageSize, Expression<Func<T, bool>> predicate);
        Task<IEnumerable<T>> GetPagedAsync<TKey>(int pageNumber, int pageSize, Expression<Func<T, TKey>> orderBy, bool ascending = true);

        // Advanced query operations
        Task<IEnumerable<TResult>> SelectAsync<TResult>(Expression<Func<T, TResult>> selector);
        Task<IEnumerable<TResult>> SelectAsync<TResult>(Expression<Func<T, bool>> predicate, Expression<Func<T, TResult>> selector);
        Task<IEnumerable<T>> WhereAsync(Expression<Func<T, bool>> predicate);
        Task<IEnumerable<T>> OrderByAsync<TKey>(Expression<Func<T, TKey>> orderBy, bool ascending = true);

        // Transaction support
        Task<T> AddWithReturnAsync(T entity);
        Task<bool> ExistsAsync(int id);
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
        Task SaveChangesAsync();
    }
}