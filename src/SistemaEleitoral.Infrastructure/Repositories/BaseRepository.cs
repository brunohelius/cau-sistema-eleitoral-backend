using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SistemaEleitoral.Domain.Entities;
using SistemaEleitoral.Domain.Interfaces;
using SistemaEleitoral.Infrastructure.Data;
using System.Linq.Expressions;

namespace SistemaEleitoral.Infrastructure.Repositories
{
    public abstract class BaseRepository<T> : IRepository<T> where T : BaseEntity
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<T> _dbSet;
        protected readonly IMemoryCache _cache;
        protected readonly ILogger _logger;
        protected readonly string _cacheKeyPrefix;

        protected BaseRepository(ApplicationDbContext context, IMemoryCache cache, ILogger logger)
        {
            _context = context;
            _dbSet = context.Set<T>();
            _cache = cache;
            _logger = logger;
            _cacheKeyPrefix = typeof(T).Name.ToLower();
        }

        #region Basic CRUD Operations

        public virtual async Task<T?> GetByIdAsync(int id)
        {
            var cacheKey = GetCacheKey("id", id);
            return await GetFromCacheOrExecuteAsync(cacheKey,
                async () => await _dbSet.FirstOrDefaultAsync(e => e.Id == id),
                TimeSpan.FromMinutes(30));
        }

        public virtual async Task<T?> GetByIdAsync(int id, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet;
            query = includes.Aggregate(query, (current, include) => current.Include(include));
            return await query.FirstOrDefaultAsync(e => e.Id == id);
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            var cacheKey = GetCacheKey("all");
            return await GetFromCacheOrExecuteAsync(cacheKey,
                async () => await _dbSet.AsNoTracking().ToListAsync(),
                TimeSpan.FromMinutes(15));
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet;
            query = includes.Aggregate(query, (current, include) => current.Include(include));
            return await query.AsNoTracking().ToListAsync();
        }

        public virtual async Task<T> AddAsync(T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            entity.DataCriacao = DateTime.UtcNow;
            var entry = await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            
            InvalidateCacheForEntity(entity.Id);
            _logger.LogInformation("Created {EntityType} with ID {EntityId}", typeof(T).Name, entity.Id);
            
            return entry.Entity;
        }

        public virtual async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));

            var entityList = entities.ToList();
            foreach (var entity in entityList)
            {
                entity.DataCriacao = DateTime.UtcNow;
            }

            await _dbSet.AddRangeAsync(entityList);
            await _context.SaveChangesAsync();
            
            InvalidateAllCache();
            _logger.LogInformation("Created {Count} {EntityType} entities", entityList.Count, typeof(T).Name);
            
            return entityList;
        }

        public virtual async Task<T> UpdateAsync(T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            entity.DataAlteracao = DateTime.UtcNow;
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
            
            InvalidateCacheForEntity(entity.Id);
            _logger.LogInformation("Updated {EntityType} with ID {EntityId}", typeof(T).Name, entity.Id);
            
            return entity;
        }

        public virtual async Task<IEnumerable<T>> UpdateRangeAsync(IEnumerable<T> entities)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));

            var entityList = entities.ToList();
            foreach (var entity in entityList)
            {
                entity.DataAlteracao = DateTime.UtcNow;
            }

            _dbSet.UpdateRange(entityList);
            await _context.SaveChangesAsync();
            
            InvalidateAllCache();
            _logger.LogInformation("Updated {Count} {EntityType} entities", entityList.Count, typeof(T).Name);
            
            return entityList;
        }

        public virtual async Task DeleteAsync(int id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity != null)
            {
                _dbSet.Remove(entity);
                await _context.SaveChangesAsync();
                
                InvalidateCacheForEntity(id);
                _logger.LogInformation("Deleted {EntityType} with ID {EntityId}", typeof(T).Name, id);
            }
        }

        public virtual async Task DeleteAsync(T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
            
            InvalidateCacheForEntity(entity.Id);
            _logger.LogInformation("Deleted {EntityType} with ID {EntityId}", typeof(T).Name, entity.Id);
        }

        public virtual async Task DeleteRangeAsync(IEnumerable<T> entities)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));

            var entityList = entities.ToList();
            _dbSet.RemoveRange(entityList);
            await _context.SaveChangesAsync();
            
            InvalidateAllCache();
            _logger.LogInformation("Deleted {Count} {EntityType} entities", entityList.Count, typeof(T).Name);
        }

        #endregion

        #region Query Operations

        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).AsNoTracking().ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet.Where(predicate);
            query = includes.Aggregate(query, (current, include) => current.Include(include));
            return await query.AsNoTracking().ToListAsync();
        }

        public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.FirstOrDefaultAsync(predicate);
        }

        public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet.Where(predicate);
            query = includes.Aggregate(query, (current, include) => current.Include(include));
            return await query.FirstOrDefaultAsync();
        }

        public virtual async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }

        public virtual async Task<int> CountAsync()
        {
            var cacheKey = GetCacheKey("count");
            return await GetFromCacheOrExecuteAsync(cacheKey,
                async () => await _dbSet.CountAsync(),
                TimeSpan.FromMinutes(10));
        }

        public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.CountAsync(predicate);
        }

        #endregion

        #region Paging Operations

        public virtual async Task<IEnumerable<T>> GetPagedAsync(int pageNumber, int pageSize)
        {
            return await _dbSet
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> GetPagedAsync(int pageNumber, int pageSize, Expression<Func<T, bool>> predicate)
        {
            return await _dbSet
                .Where(predicate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> GetPagedAsync<TKey>(int pageNumber, int pageSize, Expression<Func<T, TKey>> orderBy, bool ascending = true)
        {
            var query = ascending ? _dbSet.OrderBy(orderBy) : _dbSet.OrderByDescending(orderBy);
            
            return await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();
        }

        #endregion

        #region Advanced Query Operations

        public virtual async Task<IEnumerable<TResult>> SelectAsync<TResult>(Expression<Func<T, TResult>> selector)
        {
            return await _dbSet.Select(selector).AsNoTracking().ToListAsync();
        }

        public virtual async Task<IEnumerable<TResult>> SelectAsync<TResult>(Expression<Func<T, bool>> predicate, Expression<Func<T, TResult>> selector)
        {
            return await _dbSet.Where(predicate).Select(selector).AsNoTracking().ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> WhereAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).AsNoTracking().ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> OrderByAsync<TKey>(Expression<Func<T, TKey>> orderBy, bool ascending = true)
        {
            var query = ascending ? _dbSet.OrderBy(orderBy) : _dbSet.OrderByDescending(orderBy);
            return await query.AsNoTracking().ToListAsync();
        }

        #endregion

        #region Transaction Support

        public virtual async Task<T> AddWithReturnAsync(T entity)
        {
            return await AddAsync(entity);
        }

        public virtual async Task<bool> ExistsAsync(int id)
        {
            return await _dbSet.AnyAsync(e => e.Id == id);
        }

        public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }

        public virtual async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        #endregion

        #region Caching Helpers

        protected string GetCacheKey(params object[] keyParts)
        {
            return $"{_cacheKeyPrefix}:{string.Join(":", keyParts)}";
        }

        protected async Task<TResult> GetFromCacheOrExecuteAsync<TResult>(
            string cacheKey, 
            Func<Task<TResult>> factory, 
            TimeSpan? expiration = null)
        {
            if (_cache.TryGetValue(cacheKey, out TResult cachedValue))
            {
                return cachedValue;
            }

            var result = await factory();
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(30),
                SlidingExpiration = TimeSpan.FromMinutes(5),
                Priority = CacheItemPriority.Normal
            };

            _cache.Set(cacheKey, result, options);
            return result;
        }

        protected void InvalidateCacheForEntity(int entityId)
        {
            var keys = new[]
            {
                GetCacheKey("id", entityId),
                GetCacheKey("all"),
                GetCacheKey("count")
            };

            foreach (var key in keys)
            {
                _cache.Remove(key);
            }
        }

        protected void InvalidateAllCache()
        {
            var keys = new[]
            {
                GetCacheKey("all"),
                GetCacheKey("count")
            };

            foreach (var key in keys)
            {
                _cache.Remove(key);
            }
        }

        #endregion
    }
}