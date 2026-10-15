using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using StargateAPI.Business.Data;
using StargateAPI.Business.Queries;
using StargateAPI.Business.Commands;
using StargateAPI.Business.Dtos;
using Xunit;

namespace StargateAPI.Tests
{
    public class QueryTests
    {
        private static StargateContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<StargateContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new StargateContext(options);
        }

        [Fact]
        public async Task GetAstronautDutiesByName_PersonNotFound_Throws()
        {
            using var ctx = CreateContext("q_duties_not_found");

            var handler = new GetAstronautDutiesByNameHandler(ctx);
            var req = new GetAstronautDutiesByName { Name = "Nobody" };

            var ex = await Assert.ThrowsAsync<BadHttpRequestException>(() => handler.Handle(req, CancellationToken.None));
            Assert.Contains("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GetAstronautDutiesByName_ReturnsPersonAndDuties()
        {
            using var ctx = CreateContext("q_duties_found");

            var person = new Person { Name = "DutyPerson" };
            ctx.People.Add(person);
            await ctx.SaveChangesAsync();

            var detail = new AstronautDetail { PersonId = person.Id, CurrentRank = "Lt", CurrentDutyTitle = "Pilot", CareerStartDate = DateTime.UtcNow.AddYears(-2).Date };
            ctx.AstronautDetails.Add(detail);
            await ctx.SaveChangesAsync();

            ctx.AstronautDuties.Add(new AstronautDuty { PersonId = person.Id, DutyTitle = "A", Rank = "R", DutyStartDate = DateTime.UtcNow.AddDays(-10).Date });
            ctx.AstronautDuties.Add(new AstronautDuty { PersonId = person.Id, DutyTitle = "B", Rank = "R2", DutyStartDate = DateTime.UtcNow.AddDays(-1).Date });
            await ctx.SaveChangesAsync();

            var handler = new GetAstronautDutiesByNameHandler(ctx);
            var req = new GetAstronautDutiesByName { Name = person.Name };

            var result = await handler.Handle(req, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(person.Name, result.Person.Name);
            Assert.Equal(detail.CurrentRank, result.Person.CurrentRank);
            Assert.Equal(2, result.AstronautDuties.Count);
            // duties should be ordered descending by start date
            Assert.True(result.AstronautDuties[0].DutyStartDate >= result.AstronautDuties[1].DutyStartDate);
        }

        [Fact]
        public async Task GetPeople_ReturnsPeopleWithAndWithoutDetails()
        {
            using var ctx = CreateContext("q_people");

            var p1 = new Person { Name = "P1" };
            var p2 = new Person { Name = "P2" };
            ctx.People.AddRange(p1, p2);
            await ctx.SaveChangesAsync();

            var detail = new AstronautDetail { PersonId = p1.Id, CurrentRank = "Cpt", CurrentDutyTitle = "Lead" };
            ctx.AstronautDetails.Add(detail);
            await ctx.SaveChangesAsync();

            var handler = new GetPeopleHandler(ctx);
            var result = await handler.Handle(new GetPeople(), CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(2, result.People.Count);
            var person1 = result.People.FirstOrDefault(p => p.PersonId == p1.Id);
            var person2 = result.People.FirstOrDefault(p => p.PersonId == p2.Id);
            Assert.NotNull(person1);
            Assert.Equal("Cpt", person1.CurrentRank);
            Assert.NotNull(person2);
            Assert.Equal(string.Empty, person2.CurrentRank);
        }

        [Fact]
        public async Task GetPersonByName_ReturnsNoContent_WhenNotFound()
        {
            using var ctx = CreateContext("q_person_not_found");
            var handler = new GetPersonByNameHandler(ctx);
            var result = await handler.Handle(new GetPersonByName { Name = "Nobody" }, CancellationToken.None);
            Assert.Null(result.Person);
            Assert.Equal((int)System.Net.HttpStatusCode.NoContent, result.ResponseCode);
        }

        [Fact]
        public async Task GetPersonByName_ReturnsPerson_WhenFound()
        {
            using var ctx = CreateContext("q_person_found");

            var p = new Person { Name = "Found" };
            ctx.People.Add(p);
            await ctx.SaveChangesAsync();

            var detail = new AstronautDetail { PersonId = p.Id, CurrentRank = "Sgt", CurrentDutyTitle = "Ops" };
            ctx.AstronautDetails.Add(detail);
            await ctx.SaveChangesAsync();

            var handler = new GetPersonByNameHandler(ctx);
            var result = await handler.Handle(new GetPersonByName { Name = p.Name }, CancellationToken.None);

            Assert.NotNull(result.Person);
            Assert.Equal(p.Name, result.Person.Name);
            Assert.Equal((int)System.Net.HttpStatusCode.OK, result.ResponseCode);
        }
    }
}
