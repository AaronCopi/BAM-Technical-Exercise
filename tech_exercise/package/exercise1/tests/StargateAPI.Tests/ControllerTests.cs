using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using StargateAPI.Controllers;
using StargateAPI.Business.Queries;
using StargateAPI.Business.Commands;
using StargateAPI.Business.Dtos;
using Xunit;

namespace StargateAPI.Tests
{
    using Moq;

    public class ControllerTests
    {
        [Fact]
        public async Task PersonController_GetPeople_ReturnsConfiguredResult()
        {
            var expected = new GetPeopleResult { People = new List<PersonAstronaut> { new PersonAstronaut { PersonId = 1, Name = "X" } } };

            var mock = new Mock<IMediator>();
            mock.Setup(m => m.Send(It.IsAny<GetPeople>(), It.IsAny<CancellationToken>())).ReturnsAsync(expected);
            var mediator = mock.Object;

            var controller = new PersonController(mediator);
            var actionResult = await controller.GetPeople(CancellationToken.None);

            var obj = Assert.IsType<ObjectResult>(actionResult);
            // Controller.GetResponse takes BaseResponse, so the runtime status code is taken from BaseResponse.ResponseCode
            var asBase = Assert.IsAssignableFrom<BaseResponse>(obj.Value);
            Assert.Equal(asBase.ResponseCode, obj.StatusCode);
            Assert.Same(expected, obj.Value);
        }

        [Fact]
        public async Task AstronautDutyController_GetAstronautDutiesByName_ReturnsHandlerResult()
        {
            var personAstronaut = new StargateAPI.Business.Dtos.PersonAstronaut { PersonId = 1, Name = "DutyPerson", CurrentRank = "Lt", CurrentDutyTitle = "Pilot" };
            var duty = new StargateAPI.Business.Data.AstronautDuty { Id = 1, PersonId = 1, DutyTitle = "A", Rank = "R", DutyStartDate = DateTime.UtcNow.Date };
            var expected = new StargateAPI.Business.Queries.GetAstronautDutiesByNameResult { Person = personAstronaut, AstronautDuties = new System.Collections.Generic.List<StargateAPI.Business.Data.AstronautDuty> { duty } };

            var mock = new Mock<IMediator>();
            mock.Setup(m => m.Send(It.IsAny<StargateAPI.Business.Queries.GetAstronautDutiesByName>(), It.IsAny<CancellationToken>())).ReturnsAsync(expected);
            var mediator = mock.Object;

            var controller = new AstronautDutyController(mediator);
            var actionResult = await controller.GetAstronautDutiesByName("DutyPerson", CancellationToken.None);

            var obj = Assert.IsType<ObjectResult>(actionResult);
            var asBase = Assert.IsAssignableFrom<StargateAPI.Controllers.BaseResponse>(obj.Value);
            Assert.Equal(asBase.ResponseCode, obj.StatusCode);
            Assert.Same(expected, obj.Value);
        }

        [Fact]
        public async Task PersonController_GetPersonByName_ReturnsNoContentFromHandler()
        {
            var expected = new GetPersonByNameResult { Person = null };

            var mock2 = new Mock<IMediator>();
            mock2.Setup(m => m.Send(It.IsAny<GetPersonByName>(), It.IsAny<CancellationToken>())).ReturnsAsync(expected);
            var mediator = mock2.Object;

            var controller = new PersonController(mediator);
            var actionResult = await controller.GetPersonByName("Nobody", CancellationToken.None);

            var obj = Assert.IsType<ObjectResult>(actionResult);
            var asBase = Assert.IsAssignableFrom<BaseResponse>(obj.Value);
            Assert.Equal(asBase.ResponseCode, obj.StatusCode);
            Assert.Same(expected, obj.Value);
        }

        [Fact]
        public async Task PersonController_CreateAndUpdatePerson_ReturnsHandlerResult()
        {
            var createResult = new CreatePersonResult { Id = 5 };
            var updateResult = new UpdatePersonResult { Id = 5 };

            var mock3 = new Mock<IMediator>();
            mock3.Setup(m => m.Send(It.IsAny<CreatePerson>(), It.IsAny<CancellationToken>())).ReturnsAsync(createResult);
            mock3.Setup(m => m.Send(It.IsAny<UpdatePerson>(), It.IsAny<CancellationToken>())).ReturnsAsync(updateResult);
            var mediator = mock3.Object;

            var controller = new PersonController(mediator);

            var createAction = await controller.CreatePerson("NewName", CancellationToken.None);
            var createObj = Assert.IsType<ObjectResult>(createAction);
            Assert.Same(createResult, createObj.Value);

            var updateAction = await controller.UpdatePerson("NewName", "UpdatedName", CancellationToken.None);
            var updateObj = Assert.IsType<ObjectResult>(updateAction);
            Assert.Same(updateResult, updateObj.Value);
        }

        [Fact]
        public async Task ExceptionHandlingMiddleware_MapsExceptionToResponse()
        {
            var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<StargateAPI.Controllers.ExceptionHandlingMiddleware>>();
            // create in-memory db context and service provider
            var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<StargateAPI.Business.Data.StargateContext>()
                .UseInMemoryDatabase("middleware_logs")
                .Options;
            services.AddSingleton(new StargateAPI.Business.Data.StargateContext(options));
            var provider = services.BuildServiceProvider();

            // RequestDelegate that throws
            RequestDelegate next = ctx => throw new Exception("boom");
            var middleware = new StargateAPI.Controllers.ExceptionHandlingMiddleware(next, loggerMock.Object);
            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = provider;
            httpContext.Response.Body = new System.IO.MemoryStream();

            await middleware.InvokeAsync(httpContext);

            Assert.Equal((int)System.Net.HttpStatusCode.InternalServerError, httpContext.Response.StatusCode);
            httpContext.Response.Body.Seek(0, System.IO.SeekOrigin.Begin);
            using var reader = new System.IO.StreamReader(httpContext.Response.Body);
            var body = await reader.ReadToEndAsync();
            var resp = System.Text.Json.JsonSerializer.Deserialize<StargateAPI.Controllers.BaseResponse>(body);
            Assert.False(resp.Success);

            // verify a log entry was written to the database
            var db = provider.GetRequiredService<StargateAPI.Business.Data.StargateContext>();
            var logs = db.Logs.ToList();
            Assert.Single(logs);
            Assert.Equal("Error", logs[0].Level);
            // logger called (behavior verified by absence of exception and response content)
        }

        [Fact]
        public async Task AstronautDutyController_Create_ReturnsHandlerResult()
        {
            var created = new CreateAstronautDutyResult { Id = 9 };
            var mock5 = new Mock<IMediator>();
            mock5.Setup(m => m.Send(It.IsAny<CreateAstronautDuty>(), It.IsAny<CancellationToken>())).ReturnsAsync(created);
            var mediator = mock5.Object;

            var controller = new AstronautDutyController(mediator);
            var actionResult = await controller.CreateAstronautDuty(new CreateAstronautDuty { Name = "A", DutyTitle = "B", Rank = "C", DutyStartDate = DateTime.UtcNow }, CancellationToken.None);

            var obj = Assert.IsType<ObjectResult>(actionResult);
            Assert.Same(created, obj.Value);
        }
    }
}
