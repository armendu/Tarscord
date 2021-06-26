using AutoMapper;
using Tarscord.Core.Features.Events;
using Tarscord.Core.Models;

namespace Tarscord.Core.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Persistence.Entities.EventInfo, EventInfo>();

            CreateMap<Persistence.Entities.EventInfo, LoanInfo>();
        }
    }
}