using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using StargateAPI.Business.Data;
using StargateAPI.Business.Commands;
using Xunit;

namespace StargateAPI.Tests
{
    public class CreatePersonTests
    {
        private static StargateContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<StargateContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new StargateContext(options);
        }

        [Fact]
        public async Task PreProcessor_PersonExists_Throws()
        {
            using var ctx = CreateContext("pre_person_exists");

            var person = new Person { Name = "Existing" };
            ctx.People.Add(person);
            await ctx.SaveChangesAsync();

            var pre = new CreatePersonPreProcessor(ctx);
            var req = new CreatePerson { Name = person.Name };

            await Assert.ThrowsAsync<BadHttpRequestException>(() => pre.Process(req, CancellationToken.None));
        }

        [Fact]
        public async Task PreProcessor_PersonNotExists_DoesNotThrow()
        {
            using var ctx = CreateContext("pre_person_not_exists");

            var pre = new CreatePersonPreProcessor(ctx);
            var req = new CreatePerson { Name = "NewPerson" };

            await pre.Process(req, CancellationToken.None);
        }

        [Fact]
        public async Task Handler_CreatesPerson_ReturnsId()
        {
            using var ctx = CreateContext("handler_create_person");

            var handler = new CreatePersonHandler(ctx);

            var req = new CreatePerson { Name = "Created Person" };

            var result = await handler.Handle(req, CancellationToken.None);

            Assert.NotNull(result);
            Assert.True(result.Id > 0);

            var person = await ctx.People.FirstOrDefaultAsync(p => p.Id == result.Id);
            Assert.NotNull(person);
            Assert.Equal(req.Name, person.Name);
        }
    }
}
