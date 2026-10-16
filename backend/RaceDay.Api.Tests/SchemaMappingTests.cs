using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using Xunit;

namespace RaceDay.Api.Tests;

public sealed class SchemaMappingTests
{
    [Fact]
    public void SqlServerModel_MapsAllErEntitiesAndCriticalConstraints()
    {
        var options = new DbContextOptionsBuilder<RaceDayDbContext>()
            .UseSqlServer("Server=(local);Database=RaceDaySchemaCheck;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        using var context = new RaceDayDbContext(options);
        var model = context.Model;

        Assert.Equal(
            new[] { "Event", "EventCategory", "EventEnrolment", "Result", "Route", "User" },
            model.GetEntityTypes().Select(entity => entity.GetTableName()).OrderBy(name => name));

        var user = model.FindEntityType(typeof(User))!;
        Assert.Equal(200, user.FindProperty(nameof(User.Email))!.GetMaxLength());
        Assert.True(user.GetIndexes().Single(index => index.Properties.Single().Name == nameof(User.Email)).IsUnique);

        var eventEntity = model.FindEntityType(typeof(Event))!;
        Assert.Equal("decimal(6,2)", eventEntity.FindProperty(nameof(Event.DistanceKm))!.GetColumnType());

        var enrolment = model.FindEntityType(typeof(EventEnrolment))!;
        Assert.Equal("datetime2", enrolment.FindProperty(nameof(EventEnrolment.EnrolmentDate))!.GetColumnType());

        var result = model.FindEntityType(typeof(Result))!;
        Assert.True(result.GetIndexes().Single(index =>
            index.Properties.Single().Name == nameof(Result.EnrolmentId)).IsUnique);
        Assert.All(model.GetEntityTypes().SelectMany(entity => entity.GetForeignKeys()),
            foreignKey => Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior));
    }
}
