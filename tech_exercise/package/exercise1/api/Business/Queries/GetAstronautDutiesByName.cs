using Dapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using StargateAPI.Business.Data;
using StargateAPI.Business.Dtos;
using StargateAPI.Controllers;

namespace StargateAPI.Business.Queries
{
    public class GetAstronautDutiesByName : IRequest<GetAstronautDutiesByNameResult>
    {
        public string Name { get; set; } = string.Empty;
    }

    public class GetAstronautDutiesByNameHandler : IRequestHandler<GetAstronautDutiesByName, GetAstronautDutiesByNameResult>
    {
        private readonly StargateContext _context;

        public GetAstronautDutiesByNameHandler(StargateContext context)
        {
            _context = context;
        }

        public async Task<GetAstronautDutiesByNameResult> Handle(GetAstronautDutiesByName request, CancellationToken cancellationToken)
        {

            var result = new GetAstronautDutiesByNameResult();

            var personAstronaut = await _context.People.Join(_context.AstronautDetails, p => p.Id, ad => ad.PersonId, (p, ad) => new PersonAstronaut
            {
                PersonId = p.Id,
                Name = p.Name,
                CurrentRank = ad.CurrentRank,
                CurrentDutyTitle = ad.CurrentDutyTitle,
                CareerStartDate = ad.CareerStartDate,
                CareerEndDate = ad.CareerEndDate
            }).FirstOrDefaultAsync(p => p.Name.Equals(request.Name, StringComparison.InvariantCultureIgnoreCase), cancellationToken);

            if (personAstronaut is null)
            {
                throw new BadHttpRequestException($"Person with name '{request.Name}' not found.");
            }

            result.Person = personAstronaut;

            var duties = await _context.AstronautDuties.Where(ad => ad.PersonId == personAstronaut.PersonId).OrderByDescending(ad => ad.DutyStartDate).ToListAsync(cancellationToken);

            result.AstronautDuties = [.. duties];

            return result;

        }
    }

    public class GetAstronautDutiesByNameResult : BaseResponse
    {
        public PersonAstronaut Person { get; set; } = new PersonAstronaut();
        public List<AstronautDuty> AstronautDuties { get; set; } = new List<AstronautDuty>();
    }
}
