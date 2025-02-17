using Microsoft.EntityFrameworkCore;
using Tarscord.Core.Features.Events;
using Tarscord.Core.Persistence.Entities;

namespace Tarscord.Core.Persistence;

public class TarscordContext(DbContextOptions<TarscordContext> options) : DbContext(options)
{
    public DbSet<EventAttendee> EventAttendees { get; set; }

    public DbSet<EventInfo> EventInfos { get; set; }

    public DbSet<Loan> Loans { get; set; }
}