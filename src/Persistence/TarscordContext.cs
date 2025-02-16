using Microsoft.EntityFrameworkCore;
using Tarscord.Core.Domain;
using Tarscord.Core.Features.Events;

namespace Tarscord.Core.Persistence;

public class TarscordContext(DbContextOptions<TarscordContext> options) : DbContext(options)
{
    public DbSet<EventAttendee> EventAttendees { get; set; }

    public DbSet<EventInfo> EventInfos { get; set; }
}