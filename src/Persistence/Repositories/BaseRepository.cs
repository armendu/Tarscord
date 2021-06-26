using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tarscord.Persistence.Entities;
using Tarscord.Persistence.Exceptions;
using Tarscord.Persistence.Interfaces;

namespace Tarscord.Persistence.Repositories
{
    public class BaseRepository<T> : IBaseRepository<T>
        where T : EntityBase
    {
        protected readonly IDatabaseConnection _connection;

        public BaseRepository(IDatabaseConnection connection)
        {
            _connection = connection;
        }

        public async Task<IQueryable<T>> FindBy(Func<T, bool> predicate)
        {
            IEnumerable<T> results = await _connection.Connection.GetAllAsync<T>();

            return results.Where(predicate).AsQueryable();
        }

        public async Task<IQueryable<T>> GetAllAsync()
        {
            IEnumerable<T> items = await _connection.Connection.GetAllAsync<T>();

            return items.AsQueryable();
        }

        public async Task<T> InsertAsync(T item)
        {
            int noRowsAffected = await _connection.Connection.InsertAsync(item);

            if (noRowsAffected == 0)
            {
                throw new OperationFailedException();
            }

            return item;
        }

        public async Task<T> UpdateItem(T item)
        {
            bool modificationSucceeded = await _connection.Connection.UpdateAsync(item);

            if (!modificationSucceeded)
            {
                throw new OperationFailedException();
            }

            return item;
        }

        public async Task DeleteItem(T item)
        {
            bool deletionSucceeded = await _connection.Connection.DeleteAsync(item);

            if (!deletionSucceeded)
            {
                throw new OperationFailedException();
            }
        }
    }
}