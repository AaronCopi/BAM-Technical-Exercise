using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
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
            var middleware = new StargateAPI.Controllers.ExceptionHandlingMiddleware(ctx => throw new Exception("boom"));
            var httpContext = new DefaultHttpContext();
            httpContext.Response.Body = new System.IO.MemoryStream();

            await middleware.InvokeAsync(httpContext);

            Assert.Equal((int)System.Net.HttpStatusCode.InternalServerError, httpContext.Response.StatusCode);
            httpContext.Response.Body.Seek(0, System.IO.SeekOrigin.Begin);
            using var reader = new System.IO.StreamReader(httpContext.Response.Body);
            var body = await reader.ReadToEndAsync();
            var resp = System.Text.Json.JsonSerializer.Deserialize<StargateAPI.Controllers.BaseResponse>(body);
            Assert.False(resp.Success);
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
