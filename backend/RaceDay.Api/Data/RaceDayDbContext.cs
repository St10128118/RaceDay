using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;
using RaceRoute = RaceDay.Api.Models.Route;

namespace RaceDay.Api.Data
{
    public class RaceDayDbContext : DbContext
    {
        public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

        public DbSet<User> User => Set<User>();
        public DbSet<Event> Event => Set<Event>();
        public DbSet<EventCategory> EventCategory => Set<EventCategory>();
        public DbSet<EventEnrolment> EventEnrolment => Set<EventEnrolment>();
        public DbSet<Result> Result => Set<Result>();
        public DbSet<RaceRoute> Route => Set<RaceRoute>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new Configurations.UserConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.EventConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.EventCategoryConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.EventEnrolmentConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.ResultConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.RouteConfiguration());
        }
    }
}
