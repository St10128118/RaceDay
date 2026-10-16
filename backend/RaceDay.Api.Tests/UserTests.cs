using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using Xunit;

namespace RaceDay.Api.Tests
{
    public class UserTests
    {
        [Fact]
        public void Can_Add_User_To_InMemory_Db()
        {
            var options = new DbContextOptionsBuilder<RaceDayDbContext>()
                .UseInMemoryDatabase(databaseName: "Test_Add_User")
                .Options;

            using var context = new RaceDayDbContext(options);
            var user = new User { FirstName = "Test", LastName = "User", Email = "test@example.com", Role = "Participant", PasswordHash = "hash" };
            context.User.Add(user);
            context.SaveChanges();

            Assert.Equal(1, context.User.CountAsync().GetAwaiter().GetResult());
        }
    }
}
