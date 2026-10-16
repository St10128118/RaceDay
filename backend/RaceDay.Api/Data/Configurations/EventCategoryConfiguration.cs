using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations
{
    public class EventCategoryConfiguration : IEntityTypeConfiguration<EventCategory>
    {
        public void Configure(EntityTypeBuilder<EventCategory> builder)
        {
            builder.ToTable("EventCategory");
            builder.HasKey(c => c.CategoryId);
            builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
            builder.Property(c => c.DistanceKm).HasColumnType("decimal(6,2)");
            builder.Property(c => c.MinAge);
            builder.Property(c => c.MaxAge);

            builder.HasOne(c => c.Event)
                .WithMany(e => e.Categories)
                .HasForeignKey(c => c.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(c => c.EventId).HasDatabaseName("INDEX_EventCategory_EventId");
        }
    }
}
