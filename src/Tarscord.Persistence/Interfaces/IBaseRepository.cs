using System;
using System.Linq;
using System.Threading.Tasks;

namespace Tarscord.Persistence.Interfaces
{
    public interface IBaseRepository<T> where T : class
    {
        Task<IQueryable<T>> FindBy(Func<T, bool> predicate);
        Task<IQueryable<T>> GetAllAsync();
        Task<T> CreateAsync(T item);
        Task<T> UpdateItem(T item);
        Task DeleteItem(T item);
    }
}