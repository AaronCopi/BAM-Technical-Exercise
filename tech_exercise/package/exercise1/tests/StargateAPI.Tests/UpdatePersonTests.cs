using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using StargateAPI.Business.Data;
using StargateAPI.Business.Commands;
using Xunit;

namespace StargateAPI.Tests
{
    public class UpdatePersonTests
    {
        private static StargateContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<StargateContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new StargateContext(options);
        }

        [Fact]
        public async Task PreProcessor_PersonNotFound_Throws()
        {
            using var ctx = CreateContext("update_pre_not_found");

            var pre = new UpdatePersonPreProcessor(ctx);
            var req = new UpdatePerson { Name = "Nobody", NewName = "Someone" };

            await Assert.ThrowsAsync<BadHttpRequestException>(() => pre.Process(req, CancellationToken.None));
        }

        [Fact]
        public async Task PreProcessor_PersonExists_DoesNotThrow()
        {
            using var ctx = CreateContext("update_pre_exists");

            var person = new Person { Name = "Exist" };
            ctx.People.Add(person);
            await ctx.SaveChangesAsync();

            var pre = new UpdatePersonPreProcessor(ctx);
            var req = new UpdatePerson { Name = person.Name, NewName = "NewName" };

            await pre.Process(req, CancellationToken.None);
        }

        [Fact]
        public async Task Handler_PersonNotFound_Throws()
        {
            using var ctx = CreateContext("update_handler_not_found");

            var handler = new UpdatePersonHandler(ctx);
            var req = new UpdatePerson { Name = "NoOne", NewName = "NoOneElse" };

            await Assert.ThrowsAsync<BadHttpRequestException>(() => handler.Handle(req, CancellationToken.None));
        }

        [Fact]
        public async Task Handler_UpdatesName_ReturnsId()
        {
            using var ctx = CreateContext("update_handler_update");

            var person = new Person { Name = "Original" };
            ctx.People.Add(person);
            await ctx.SaveChangesAsync();

            var handler = new UpdatePersonHandler(ctx);
            var req = new UpdatePerson { Name = person.Name, NewName = "Updated" };

            var result = await handler.Handle(req, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(person.Id, result.Id);

            var updated = await ctx.People.FirstOrDefaultAsync(p => p.Id == person.Id);
            Assert.NotNull(updated);
            Assert.Equal(req.NewName, updated.Name);
        }
    }
}
