using Microsoft.EntityFrameworkCore;
using Tarscord.Core.Persistence.Entities;

namespace Tarscord.Core.Persistence;

public class TarscordContext(DbContextOptions<TarscordContext> options) : DbContext(options)
{
    public DbSet<EventAttendee> EventAttendees { get; set; } = null!;

    public DbSet<EventInfo> EventInfos { get; set; } = null!;

    public DbSet<Loan> Loans { get; set; } = null!;

    public DbSet<Reminder> Reminders { get; set; } = null!;

    public DbSet<Restriction> Restrictions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // PostgreSQL has no unsigned types: without this, ulong maps to numeric(20,0), not BIGINT.
        modelBuilder.Entity<EventInfo>().Property(eventInfo => eventInfo.EventOrganizerId).HasConversion<long>();
        modelBuilder.Entity<EventAttendee>().Property(attendee => attendee.AttendeeId).HasConversion<long>();
        modelBuilder.Entity<Loan>().Property(loan => loan.LoanedFromId).HasConversion<long>();
        modelBuilder.Entity<Loan>().Property(loan => loan.LoanedToId).HasConversion<long>();
        modelBuilder.Entity<Reminder>().Property(reminder => reminder.UserId).HasConversion<long>();
        modelBuilder.Entity<Reminder>().Property(reminder => reminder.ChannelId).HasConversion<long>();
        modelBuilder.Entity<Restriction>().Property(restriction => restriction.UserId).HasConversion<long>();
        modelBuilder.Entity<Restriction>().Property(restriction => restriction.ChannelId).HasConversion<long>();

        // Stored as its name, so the column is readable without the C# enum.
        modelBuilder.Entity<Restriction>().Property(restriction => restriction.Kind).HasConversion<string>();

        modelBuilder.Entity<Loan>().Property(loan => loan.AmountLoaned).HasPrecision(18, 2);
        modelBuilder.Entity<Loan>().Property(loan => loan.AmountPayed).HasPrecision(18, 2);

        modelBuilder.Entity<EventAttendee>()
            .HasIndex(attendee => new { attendee.EventInfoId, attendee.AttendeeId })
            .IsUnique();
    }
}
