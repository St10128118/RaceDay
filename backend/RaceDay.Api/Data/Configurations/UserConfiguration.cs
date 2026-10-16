using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("User");
            builder.HasKey(u => u.UserId);
            builder.Property(u => u.Role).HasMaxLength(50).IsRequired();
            builder.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
            builder.Property(u => u.LastName).HasMaxLength(100).IsRequired();
            builder.Property(u => u.Email).HasMaxLength(200).IsRequired();
            builder.HasIndex(u => u.Email).IsUnique();
            builder.Property(u => u.PasswordHash).HasMaxLength(500).IsRequired();
            builder.Property(u => u.Phone).HasMaxLength(50);
            builder.Property(u => u.DateOfBirth).HasColumnType("date");
            builder.Property(u => u.Gender).HasMaxLength(20);
            builder.Property(u => u.IdNumber).HasMaxLength(50);
            builder.Property(u => u.EmergencyContactName).HasMaxLength(200);
            builder.Property(u => u.EmergencyContactPhone).HasMaxLength(50);
            builder.Property(u => u.OrganisationName).HasMaxLength(200);
        }
    }
}
