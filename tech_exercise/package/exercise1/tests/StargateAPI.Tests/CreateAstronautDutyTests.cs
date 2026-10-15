using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using StargateAPI.Business.Data;
using StargateAPI.Business.Commands;
using Xunit;

namespace StargateAPI.Tests
{
    public class CreateAstronautDutyTests
    {
        private static StargateContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<StargateContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new StargateContext(options);
        }

        [Fact]
        public async Task PreProcessor_MostRecentDutyLater_Throws()
        {
            using var ctx = CreateContext("pre_most_recent_later");

            var person = new Person { Name = "Alice" };
            ctx.People.Add(person);
            await ctx.SaveChangesAsync();

            var dutyDate = DateTime.UtcNow.Date;
            // Most recent duty has same start date (>= request) but different title
            ctx.AstronautDuties.Add(new AstronautDuty { PersonId = person.Id, DutyTitle = "Engineer", DutyStartDate = dutyDate });
            await ctx.SaveChangesAsync();

            var pre = new CreateAstronautDutyPreProcessor(ctx);

            var req = new CreateAstronautDuty
            {
                Name = person.Name,
                DutyTitle = "Pilot",
                Rank = "Lieutenant",
                DutyStartDate = dutyDate
            };

            await Assert.ThrowsAsync<BadHttpRequestException>(() => pre.Process(req, CancellationToken.None));
        }

        [Fact]
        public async Task PreProcessor_ValidRequest_DoesNotThrow()
        {
            using var ctx = CreateContext("pre_valid_request");

            var person = new Person { Name = "Bob" };
            ctx.People.Add(person);
            await ctx.SaveChangesAsync();

            // Existing duty earlier than new request
            ctx.AstronautDuties.Add(new AstronautDuty { PersonId = person.Id, DutyTitle = "Engineer", DutyStartDate = DateTime.UtcNow.AddYears(-2).Date });
            await ctx.SaveChangesAsync();

            var pre = new CreateAstronautDutyPreProcessor(ctx);

            var req = new CreateAstronautDuty
            {
                Name = person.Name,
                DutyTitle = "Pilot",
                Rank = "Lieutenant",
                DutyStartDate = DateTime.UtcNow.Date
            };

            await pre.Process(req, CancellationToken.None);
        }

        [Fact]
        public async Task Handler_CreatesRetirementDetail_WhenNoDetailExists()
        {
            using var ctx = CreateContext("handler_retire_no_detail");

            var person = new Person { Name = "Carl" };
            ctx.People.Add(person);
            await ctx.SaveChangesAsync();

            var handler = new CreateAstronautDutyHandler(ctx);

            var start = DateTime.UtcNow.Date;
            var req = new CreateAstronautDuty
            {
                Name = person.Name,
                DutyTitle = "retired",
                Rank = "Colonel",
                DutyStartDate = start
            };

            var result = await handler.Handle(req, CancellationToken.None);

            Assert.NotNull(result);
            var detail = await ctx.AstronautDetails.FirstOrDefaultAsync(d => d.PersonId == person.Id);
            Assert.NotNull(detail);
            Assert.Equal(start, detail.CareerStartDate);
            Assert.Equal(start, detail.CareerEndDate);
        }

        [Fact]
        public async Task Handler_Retiring_SetsCareerEndDateMinusOne_WhenDetailExists()
        {
            using var ctx = CreateContext("handler_retire_with_detail");

            var person = new Person { Name = "Dana" };
            ctx.People.Add(person);
            await ctx.SaveChangesAsync();

            var existingDetail = new AstronautDetail { PersonId = person.Id, CurrentDutyTitle = "Pilot", CurrentRank = "Lieutenant", CareerStartDate = DateTime.UtcNow.AddYears(-5).Date };
            ctx.AstronautDetails.Add(existingDetail);
            await ctx.SaveChangesAsync();

            var handler = new CreateAstronautDutyHandler(ctx);

            var start = DateTime.UtcNow.Date;
            var req = new CreateAstronautDuty
            {
                Name = person.Name,
                DutyTitle = "RETIRED",
                Rank = "Colonel",
                DutyStartDate = start
            };

            var result = await handler.Handle(req, CancellationToken.None);

            Assert.NotNull(result);
            var detail = await ctx.AstronautDetails.FirstOrDefaultAsync(d => d.PersonId == person.Id);
            Assert.NotNull(detail);
            Assert.Equal(start.AddDays(-1).Date, detail.CareerEndDate);
        }

        [Fact]
        public async Task Handler_Unretire_ClearsCareerEndDate()
        {
            using var ctx = CreateContext("handler_unretire");

            var person = new Person { Name = "Eve" };
            ctx.People.Add(person);
            await ctx.SaveChangesAsync();

            var existingDetail = new AstronautDetail { PersonId = person.Id, CurrentDutyTitle = "Commander", CurrentRank = "Captain", CareerStartDate = DateTime.UtcNow.AddYears(-10).Date, CareerEndDate = DateTime.UtcNow.AddYears(-1).Date };
            ctx.AstronautDetails.Add(existingDetail);
            await ctx.SaveChangesAsync();

            var handler = new CreateAstronautDutyHandler(ctx);

            var start = DateTime.UtcNow.Date;
            var req = new CreateAstronautDuty
            {
                Name = person.Name,
                DutyTitle = "Commander",
                Rank = "Captain",
                DutyStartDate = start
            };

            var result = await handler.Handle(req, CancellationToken.None);

            Assert.NotNull(result);
            var detail = await ctx.AstronautDetails.FirstOrDefaultAsync(d => d.PersonId == person.Id);
            Assert.NotNull(detail);
            Assert.Null(detail.CareerEndDate);
        }

        [Fact]
        public async Task PreProcessor_PersonNotFound_Throws()
        {
            using var ctx = CreateContext("pre_person_not_found");

            var pre = new CreateAstronautDutyPreProcessor(ctx);

            var req = new CreateAstronautDuty
            {
                Name = "NonExistent",
                DutyTitle = "Pilot",
                Rank = "Lieutenant",
                DutyStartDate = DateTime.UtcNow
            };

            await Assert.ThrowsAsync<BadHttpRequestException>(() => pre.Process(req, CancellationToken.None));
        }

        [Fact]
        public async Task PreProcessor_DuplicateDuty_Throws()
        {
            using var ctx = CreateContext("pre_duplicate_duty");

            var person = new Person { Name = "John Doe" };
            ctx.People.Add(person);
            await ctx.SaveChangesAsync();

            var dutyDate = DateTime.UtcNow.Date;
            ctx.AstronautDuties.Add(new AstronautDuty { PersonId = person.Id, DutyTitle = "Engineer", DutyStartDate = dutyDate });
            await ctx.SaveChangesAsync();

            var pre = new CreateAstronautDutyPreProcessor(ctx);

            var req = new CreateAstronautDuty
            {
                Name = person.Name,
                DutyTitle = "Engineer",
                Rank = "Commander",
                DutyStartDate = dutyDate
            };

            await Assert.ThrowsAsync<BadHttpRequestException>(() => pre.Process(req, CancellationToken.None));
        }

        [Fact]
        public async Task Handler_CreatesNewDutyAndUpdatesPrevious()
        {
            using var ctx = CreateContext("handler_create_and_update");

            var person = new Person { Name = "Jane Doe" };
            ctx.People.Add(person);
            await ctx.SaveChangesAsync();

            // Existing duty that should be closed when new duty is created
            ctx.AstronautDuties.Add(new AstronautDuty { PersonId = person.Id, DutyTitle = "Medic", Rank = "Ensign", DutyStartDate = DateTime.UtcNow.AddYears(-1).Date });
            await ctx.SaveChangesAsync();

            var handler = new CreateAstronautDutyHandler(ctx);

            var newStart = DateTime.UtcNow.Date;
            var req = new CreateAstronautDuty
            {
                Name = person.Name,
                DutyTitle = "Chief Medic",
                Rank = "Lieutenant",
                DutyStartDate = newStart
            };

            var result = await handler.Handle(req, CancellationToken.None);

            Assert.NotNull(result);
            Assert.True(result.Id.HasValue);

            var duties = ctx.AstronautDuties.Where(d => d.PersonId == person.Id).ToList();
            Assert.Equal(2, duties.Count);

            var previous = duties.OrderBy(d => d.DutyStartDate).First();
            Assert.Equal(newStart.AddDays(-1).Date, previous.DutyEndDate);

            var created = duties.OrderByDescending(d => d.DutyStartDate).First();
            Assert.Equal(req.DutyTitle, created.DutyTitle);
        }
    }
}
