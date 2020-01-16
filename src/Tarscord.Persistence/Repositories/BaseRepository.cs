using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper.Contrib.Extensions;
using Tarscord.Persistence.Interfaces;

namespace Tarscord.Persistence.Repositories
{
    public class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        protected readonly IDatabaseConnection _connection;

        public BaseRepository(IDatabaseConnection connection)
        {
            _connection = connection;
        }

        public async Task<IQueryable<T>> FindBy(Func<T, bool> predicate)
        {
            IEnumerable<T> results = await _connection.Connection.GetAllAsync<T>();
            results = results?.Where(predicate);

            return results?.AsQueryable();
        }

        public async Task<IQueryable<T>> GetAllAsync()
        {
            IEnumerable<T> items = await _connection.Connection.GetAllAsync<T>();

            return items.AsQueryable();
        }

        public async Task<T> InsertAsync(T item)
        {
            int noRowsAffected = await _connection.Connection.InsertAsync(item);

            return noRowsAffected != 0 ? item : null;
        }

        public async Task<T> UpdateItem(T item)
        {
            await _connection.Connection.UpdateAsync(item);

            return item;
        }

        public async Task DeleteItem(T item)
        {
            await _connection.Connection.DeleteAsync(item);
        }
    }
}