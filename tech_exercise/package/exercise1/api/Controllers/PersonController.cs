using MediatR;
using Microsoft.AspNetCore.Mvc;
using StargateAPI.Business.Commands;
using StargateAPI.Business.Queries;
using System.Net;

namespace StargateAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PersonController : ControllerBase
    {
        private readonly IMediator _mediator;
        public PersonController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("")]
        public async Task<IActionResult> GetPeople(CancellationToken cancellationToken)
        {
            var ct = cancellationToken == default ? (HttpContext?.RequestAborted ?? CancellationToken.None) : cancellationToken;
            var result = await _mediator.Send(new GetPeople(), ct);
            return this.GetResponse(result);
        }

        [HttpGet("{name}")]
        public async Task<IActionResult> GetPersonByName(string name, CancellationToken cancellationToken)
        {
            var ct = cancellationToken == default ? (HttpContext?.RequestAborted ?? CancellationToken.None) : cancellationToken;
            var result = await _mediator.Send(new GetPersonByName()
            {
                Name = name
            }, ct);
            return this.GetResponse(result);
        }

        [HttpPost("")]
        public async Task<IActionResult> CreatePerson([FromBody] string name, CancellationToken cancellationToken)
        {
            var ct = cancellationToken == default ? (HttpContext?.RequestAborted ?? CancellationToken.None) : cancellationToken;
            var result = await _mediator.Send(new CreatePerson()
            {
                Name = name
            }, ct);
            return this.GetResponse(result);
        }

        [HttpPut("{name}")]
        public async Task<IActionResult> UpdatePerson(string name, [FromBody] string newName, CancellationToken cancellationToken)
        {
            var ct = cancellationToken == default ? (HttpContext?.RequestAborted ?? CancellationToken.None) : cancellationToken;
            var result = await _mediator.Send(new UpdatePerson()
            {
                Name = name,
                NewName = newName
            }, ct);
            return this.GetResponse(result);
        }
    }
}
