﻿using System;
using Tarscord.Core.Domain;
using System.Threading.Tasks;
using Dapper.Contrib.Extensions;
using System.Collections.Generic;
using Tarscord.Core.Persistence.Interfaces;

namespace Tarscord.Core.Persistence.Repositories;

public class EventAttendeesRepository : BaseRepository<EventAttendee>, IEventAttendeesRepository
{
    public EventAttendeesRepository(IDatabaseConnection connection)
        : base(connection)
    {
    }

    public async Task<IList<EventAttendee>> InsertAllAsync(IList<EventAttendee> items)
    {
        int noRowsAffected = await _connection.Connection.InsertAsync(items);

        if (noRowsAffected == 0)
        {
            throw new OperationCanceledException();
        }

        return items;
    }
}