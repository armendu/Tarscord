using AutoMapper;
using Tarscord.Core.Domain;
using Tarscord.Core.Features.EventAttendees;

namespace Tarscord.Core.Helpers
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            #region Events

            CreateMap<Tarscord.Core.Features.Events.Create.EventInfo, EventInfo>();

            CreateMap<Update.Attendee, EventAttendee>()
                .ForMember(x => x.Id, opt => opt.Ignore())
                .ForMember(x => x.Created, opt => opt.Ignore())
                .ForMember(x => x.Updated, opt => opt.Ignore());

            #endregion

            #region Loans

            //CreateMap<Tarscord.Core.Features.Loans.Create.Loan, Loan>();

            #endregion
        }
    }
}