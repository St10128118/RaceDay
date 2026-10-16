using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations
{
    public class ResultConfiguration : IEntityTypeConfiguration<Result>
    {
        public void Configure(EntityTypeBuilder<Result> builder)
        {
            builder.ToTable("Result");
            builder.HasKey(r => r.ResultId);
            builder.Property(r => r.FinishTimeSeconds);
            builder.Property(r => r.PositionOverall);
            builder.Property(r => r.PositionCategory);
            builder.Property(r => r.PositionGender);
            builder.Property(r => r.Status).HasMaxLength(50).IsRequired();

            builder.HasOne(r => r.Enrolment)
                .WithOne(e => e.Result)
                .HasForeignKey<Result>(r => r.EnrolmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(r => r.EnrolmentId).IsUnique().HasDatabaseName("INDEX_Result_EnrolmentId");
        }
    }
}
