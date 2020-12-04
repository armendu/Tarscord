using AutoMapper;

namespace Tarscord.Core.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Persistence.Entities.EventInfo, Application.Models.EventInfo>();
            // Additional mappings here...
        }
    }
}