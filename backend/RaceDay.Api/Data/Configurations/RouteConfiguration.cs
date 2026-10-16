using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;
using RaceRoute = RaceDay.Api.Models.Route;

namespace RaceDay.Api.Data.Configurations
{
    public class RouteConfiguration : IEntityTypeConfiguration<RaceRoute>
    {
        public void Configure(EntityTypeBuilder<RaceRoute> builder)
        {
            builder.ToTable("Route");
            builder.HasKey(r => r.RouteId);
            builder.Property(r => r.Name).HasMaxLength(200).IsRequired();
            builder.Property(r => r.DistanceKm).HasColumnType("decimal(6,2)").IsRequired();
            builder.Property(r => r.GPXUrl).HasMaxLength(500);
            builder.Property(r => r.ElevationGainMeters);

            builder.HasOne(r => r.Event)
                .WithMany(e => e.Routes)
                .HasForeignKey(r => r.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(r => r.EventId).HasDatabaseName("INDEX_Route_EventId");
        }
    }
}
