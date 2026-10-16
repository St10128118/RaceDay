using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations
{
    public class EventEnrolmentConfiguration : IEntityTypeConfiguration<EventEnrolment>
    {
        public void Configure(EntityTypeBuilder<EventEnrolment> builder)
        {
            builder.ToTable("EventEnrolment");
            builder.HasKey(e => e.EnrolmentId);
            builder.Property(e => e.EnrolmentDate).HasColumnType("datetime2").HasDefaultValueSql("SYSUTCDATETIME()");
            builder.Property(e => e.PaymentStatus).HasMaxLength(50).IsRequired();
            builder.Property(e => e.BibNumber).HasMaxLength(50);
            builder.Property(e => e.IsConfirmed).HasDefaultValue(false);

            builder.HasOne(e => e.Event)
                .WithMany(ev => ev.Enrolments)
                .HasForeignKey(e => e.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Category)
                .WithMany(c => c.Enrolments)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Participant)
                .WithMany(u => u.Enrolments)
                .HasForeignKey(e => e.ParticipantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(e => e.EventId).HasDatabaseName("INDEX_Enrolment_EventId");
            builder.HasIndex(e => e.ParticipantId).HasDatabaseName("INDEX_Enrolment_ParticipantId");
        }
    }
}
