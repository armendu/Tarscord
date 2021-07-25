using AutoMapper;
using Tarscord.Core.Domain;
using Tarscord.Core.Features.Events;

namespace Tarscord.Core.Helpers
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            #region Events

            CreateMap<Create.EventInfo, EventInfo>();

            #endregion
        }
    }
}