using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations
{
    public class EventConfiguration : IEntityTypeConfiguration<Event>
    {
        public void Configure(EntityTypeBuilder<Event> builder)
        {
            builder.ToTable("Event");
            builder.HasKey(e => e.EventId);
            builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
            builder.Property(e => e.Description);
            builder.Property(e => e.EventType).HasMaxLength(50).IsRequired();
            builder.Property(e => e.Location).HasMaxLength(200).IsRequired();
            builder.Property(e => e.City).HasMaxLength(100).IsRequired();
            builder.Property(e => e.Province).HasMaxLength(100).IsRequired();
            builder.Property(e => e.DistanceKm).HasColumnType("decimal(6,2)").IsRequired();
            builder.Property(e => e.EventDateTime).HasColumnType("datetime2").IsRequired();
            builder.Property(e => e.Status).HasMaxLength(50).IsRequired();
            builder.Property(e => e.MaxParticipants);

            builder.HasOne(e => e.Organiser)
                .WithMany(u => u.EventsOrganised)
                .HasForeignKey(e => e.OrganiserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(e => e.OrganiserId).HasDatabaseName("INDEX_Event_OrganiserId");
            builder.HasIndex(e => e.EventDateTime).HasDatabaseName("INDEX_Event_EventDateTime");
        }
    }
}
