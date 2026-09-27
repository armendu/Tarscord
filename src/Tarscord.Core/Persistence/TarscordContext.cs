using Microsoft.EntityFrameworkCore;
using Tarscord.Core.Persistence.Entities;

namespace Tarscord.Core.Persistence;

public class TarscordContext(DbContextOptions<TarscordContext> options) : DbContext(options)
{
    public DbSet<EventAttendee> EventAttendees { get; set; }

    public DbSet<EventInfo> EventInfos { get; set; }

    public DbSet<Loan> Loans { get; set; }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Discord ids are ulong and PostgreSQL has no unsigned types, so without a conversion EF maps
        // them to numeric(20,0) while the columns are BIGINT. Snowflakes stay well below 2^63, so
        // going through long loses nothing.
        modelBuilder.Entity<EventInfo>().Property(eventInfo => eventInfo.EventOrganizerId).HasConversion<long>();
        modelBuilder.Entity<EventAttendee>().Property(attendee => attendee.AttendeeId).HasConversion<long>();
        modelBuilder.Entity<Loan>().Property(loan => loan.LoanedFromId).HasConversion<long>();
        modelBuilder.Entity<Loan>().Property(loan => loan.LoanedToId).HasConversion<long>();
        modelBuilder.Entity<User>().Property(user => user.DiscordId).HasConversion<long>();

        modelBuilder.Entity<Loan>().Property(loan => loan.AmountLoaned).HasPrecision(18, 2);
        modelBuilder.Entity<Loan>().Property(loan => loan.AmountPayed).HasPrecision(18, 2);

        modelBuilder.Entity<User>()
            .HasIndex(user => user.DiscordId)
            .IsUnique();

        modelBuilder.Entity<EventAttendee>()
            .HasIndex(attendee => new { attendee.EventInfoId, attendee.AttendeeId })
            .IsUnique();
    }
}
