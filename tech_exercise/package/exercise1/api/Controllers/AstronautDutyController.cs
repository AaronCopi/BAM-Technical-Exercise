using MediatR;
using Microsoft.AspNetCore.Mvc;
using StargateAPI.Business.Commands;
using StargateAPI.Business.Queries;
using System.Net;

namespace StargateAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AstronautDutyController : ControllerBase
    {
        private readonly IMediator _mediator;
        public AstronautDutyController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{name}")]
        public async Task<IActionResult> GetAstronautDutiesByName(string name, CancellationToken cancellationToken)
        {
            var ct = cancellationToken == default ? (HttpContext?.RequestAborted ?? CancellationToken.None) : cancellationToken;
            var result = await _mediator.Send(new GetAstronautDutiesByName()
            {
                Name = name
            }, ct);

            return this.GetResponse(result);
        }
        [HttpPost("")]
        public async Task<IActionResult> CreateAstronautDuty([FromBody] CreateAstronautDuty request, CancellationToken cancellationToken)
        {
            var ct = cancellationToken == default ? (HttpContext?.RequestAborted ?? CancellationToken.None) : cancellationToken;
            var result = await _mediator.Send(request, ct);
            return this.GetResponse(result);
        }
    }
}
