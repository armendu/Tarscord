using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tarscord.Persistence.Interfaces
{
    public interface IBaseRepository<T>
        where T : class
    {
        Task<IEnumerable<T>> FindBy(Func<T, bool> predicate);

        Task<IEnumerable<T>> GetAllAsync();

        Task<T> InsertAsync(T item);

        Task<T> UpdateItem(T item);

        Task DeleteItem(T item);
    }
}