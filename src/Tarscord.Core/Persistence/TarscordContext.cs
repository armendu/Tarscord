using Microsoft.EntityFrameworkCore;
using Tarscord.Core.Persistence.Entities;

namespace Tarscord.Core.Persistence;

public class TarscordContext(DbContextOptions<TarscordContext> options) : DbContext(options)
{
    public DbSet<EventAttendee> EventAttendees { get; set; }

    public DbSet<EventInfo> EventInfos { get; set; }

    public DbSet<Loan> Loans { get; set; }

    public DbSet<Reminder> Reminders { get; set; }

    public DbSet<Restriction> Restrictions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Discord ids are ulong and PostgreSQL has no unsigned types, so without a conversion EF maps
        // them to numeric(20,0) while the columns are BIGINT. Snowflakes stay well below 2^63, so
        // going through long loses nothing.
        modelBuilder.Entity<EventInfo>().Property(eventInfo => eventInfo.EventOrganizerId).HasConversion<long>();
        modelBuilder.Entity<EventAttendee>().Property(attendee => attendee.AttendeeId).HasConversion<long>();
        modelBuilder.Entity<Loan>().Property(loan => loan.LoanedFromId).HasConversion<long>();
        modelBuilder.Entity<Loan>().Property(loan => loan.LoanedToId).HasConversion<long>();
        modelBuilder.Entity<Reminder>().Property(reminder => reminder.UserId).HasConversion<long>();
        modelBuilder.Entity<Reminder>().Property(reminder => reminder.ChannelId).HasConversion<long>();
        modelBuilder.Entity<Restriction>().Property(restriction => restriction.UserId).HasConversion<long>();
        modelBuilder.Entity<Restriction>().Property(restriction => restriction.ChannelId).HasConversion<long>();

        // Stored as its name, so the column reads as 'mute' rather than as an integer whose meaning
        // lives only in C#.
        modelBuilder.Entity<Restriction>().Property(restriction => restriction.Kind).HasConversion<string>();

        modelBuilder.Entity<Loan>().Property(loan => loan.AmountLoaned).HasPrecision(18, 2);
        modelBuilder.Entity<Loan>().Property(loan => loan.AmountPayed).HasPrecision(18, 2);

        modelBuilder.Entity<EventAttendee>()
            .HasIndex(attendee => new { attendee.EventInfoId, attendee.AttendeeId })
            .IsUnique();
    }
}
