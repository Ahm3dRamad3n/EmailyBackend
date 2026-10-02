using Emaily.DAL.Entities;
using Emaily.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Emaily.DAL.Repositories
{
    public class GenericRepository<T>(EmailyDbContext context) : IGenericRepository<T> where T : class
    {
        protected readonly EmailyDbContext _context = context;
        protected readonly DbSet<T> _dbSet = context.Set<T>();

        // ========================================================
        // 1. Pagination 
        // ========================================================
        public async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
            Expression<Func<T, bool>> criteria, int pageNumber, int pageSize,
            Expression<Func<T, object>> orderBy, bool isDescending)
        {
            var query = _dbSet.AsNoTracking().Where(criteria);
            var totalCount = await query.CountAsync();

            query = isDescending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);

            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, totalCount);
        }
        public async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
            Expression<Func<T, bool>> criteria, int pageNumber, int pageSize,
            Expression<Func<T, object>> orderBy, bool isDescending,
            Func<IQueryable<T>, IQueryable<T>> includes)
        {
            IQueryable<T> query = _dbSet.AsNoTracking().Where(criteria);
            var totalCount = await _dbSet.CountAsync(criteria);

            if (includes != null) query = includes(query);

            if (orderBy != null)
                query = isDescending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);

            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, totalCount);
        }

        // ========================================================
        // 2. Find Single Entity (بدون AsNoTracking لتسمح بالتعديل لاحقاً)
        // ========================================================
        public async Task<T?> FindAsync(Expression<Func<T, bool>> criteria)
            => await _dbSet.FirstOrDefaultAsync(criteria);
        public async Task<T?> FindAsync(Expression<Func<T, bool>> criteria, Func<IQueryable<T>, IQueryable<T>> includes)
        {
            IQueryable<T> query = _dbSet;
            if (includes != null) query = includes(query);
            return await query.FirstOrDefaultAsync(criteria);
        }

        public async Task<T?> GetByIdAsync(object id)
        {
            return await _dbSet.FindAsync(id);
        }

        // ========================================================
        // 3. Find All Entities 
        // ========================================================
        public async Task<IEnumerable<T>> GetAllAsync(bool disableTracking = true)
        {
            IQueryable<T> query = _dbSet;
            if (disableTracking) query = query.AsNoTracking();
            return await query.ToListAsync();
        }

        public async Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> criteria, bool disableTracking = true)
        {
            IQueryable<T> query = _dbSet;
            if (disableTracking) query = query.AsNoTracking();
            return await query.Where(criteria).ToListAsync(); 
        }

        public async Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> criteria, Func<IQueryable<T>, IQueryable<T>> includes, bool disableTracking = true)
        {
            IQueryable<T> query = _dbSet;
            if (disableTracking) query = query.AsNoTracking();
            if (includes != null) query = includes(query);
            return await query.Where(criteria).ToListAsync();
        }

        // ========================================================
        // 4. Select / Projection 
        // ========================================================
        public async Task<IEnumerable<TResult>> SelectAsync<TResult>(Expression<Func<T, TResult>> selector)
        {
            return await _dbSet.AsNoTracking().Select(selector).ToListAsync();
        }

        public async Task<IEnumerable<TResult>> SelectWhereAsync<TResult>(
            Expression<Func<T, TResult>> selector, Expression<Func<T, bool>> criteria)
        {
            return await _dbSet.AsNoTracking().Where(criteria).Select(selector).ToListAsync();
        }

        public async Task<IEnumerable<TResult>> SelectWhereAsync<TResult>(
            Expression<Func<T, TResult>> selector,
            Expression<Func<T, bool>> criteria,
            Func<IQueryable<T>, IQueryable<T>> includes)
        {
            IQueryable<T> query = _dbSet.AsNoTracking().Where(criteria);
            if (includes != null) query = includes(query);
            return await query.Select(selector).ToListAsync();
        }

        // ========================================================
        // 5. Aggregate & Commands
        // ========================================================
        public async Task<int> CountAsync(Expression<Func<T, bool>> criteria)
            => await _dbSet.CountAsync(criteria);

        public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);
        public async Task AddRangeAsync(IEnumerable<T> entities) => await _dbSet.AddRangeAsync(entities);
        public void Update(T entity) => _dbSet.Update(entity);
        public void Delete(T entity) => _dbSet.Remove(entity);
    }
}