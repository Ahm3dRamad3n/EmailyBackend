
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Emaily.DAL.Interfaces
{
    public interface IGenericRepository<T> where T : class
    {
        // 1. Pagination Overloads
        Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
            Expression<Func<T, bool>> criteria,
            int pageNumber,
            int pageSize,
            Expression<Func<T, object>> orderBy,
            bool isDescending);
        Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
            Expression<Func<T, bool>> criteria,
            int pageNumber,
            int pageSize,
            Expression<Func<T, object>> orderBy,
            bool isDescending,
            Func<IQueryable<T>, IQueryable<T>> includes);

        // 2. Find Single Entity Overloads
        Task<T?> FindAsync(Expression<Func<T, bool>> criteria);
        Task<T?> FindAsync(Expression<Func<T, bool>> criteria, Func<IQueryable<T>, IQueryable<T>> includes);
        Task<T?> GetByIdAsync(object id);

        // 3. Find All Entities Overloads
        Task<IEnumerable<T>> GetAllAsync(bool disableTracking = true);
        Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> criteria, bool disableTracking = true);
        Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> criteria, Func<IQueryable<T>, IQueryable<T>> includes, bool disableTracking = true);

        // 4. Select (Projection) Overloads
        Task<IEnumerable<TResult>> SelectAsync<TResult>(
            Expression<Func<T, TResult>> selector);
        Task<IEnumerable<TResult>> SelectWhereAsync<TResult>(
            Expression<Func<T, TResult>> selector, Expression<Func<T, bool>> criteria);
        Task<IEnumerable<TResult>> SelectWhereAsync<TResult>(
            Expression<Func<T, TResult>> selector, Expression<Func<T, bool>> criteria, Func<IQueryable<T>, IQueryable<T>> includes);

        // 5. Aggregate & Write Operations
        Task<int> CountAsync(Expression<Func<T, bool>> criteria);
        Task AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);
        void Update(T entity);
        void Delete(T entity);
    }
}