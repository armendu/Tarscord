using AutoMapper;
using Tarscord.Common.Models;

namespace Tarscord.Core.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Persistence.Entities.EventInfo, EventInfo>();
            // Additional mappings here...
        }
    }
}