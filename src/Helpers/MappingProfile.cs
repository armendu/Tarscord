using AutoMapper;
using Tarscord.Core.Domain;

using UpdateEventAttendance = Tarscord.Core.Features.EventAttendees.Update.EventAttendees;
using CreateEventInfo = Tarscord.Core.Features.Events.Create.EventInfo;
using CreateLoan = Tarscord.Core.Features.Loans.Create.Loan;

namespace Tarscord.Core.Helpers
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            #region Events

            CreateMap<CreateEventInfo, EventInfo>();

            CreateMap<UpdateEventAttendance, EventAttendee>();

            #endregion

            #region Loans

            CreateMap<CreateLoan, Loan>();

            #endregion
        }
    }
}