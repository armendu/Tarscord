using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Tarscord.Persistence.Interfaces
{
    public interface IBaseRepository<T> where T : class
    {
        IQueryable<T> FindBy(Expression<Func<T, bool>> predicate);
        IQueryable<T> GetAllAsync();
        Task<T> CreateAsync(T item);
        Task<T> UpdateItem(T item);
        Task DeleteItem(T item);
    }
}