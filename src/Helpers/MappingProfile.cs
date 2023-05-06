using AutoMapper;
using Tarscord.Core.Domain;
using Tarscord.Core.Features.Loans;
using LoanUpdate = Tarscord.Core.Features.Loans.Update;
using EventUpdate = Tarscord.Core.Features.EventAttendees.Update;

namespace Tarscord.Core.Helpers;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        #region Events

        CreateMap<Tarscord.Core.Features.Events.Create.EventInfo, EventInfo>();

        CreateMap<EventUpdate.Attendee, EventAttendee>()
            .ForMember(x => x.Id, opt => opt.Ignore())
            .ForMember(x => x.Created, opt => opt.Ignore())
            .ForMember(x => x.Updated, opt => opt.Ignore());

        #endregion

        #region Loans

        CreateMap<Tarscord.Core.Features.Loans.Create.Loan, Loan>()
            .ForMember(x => x.AmountLoaned, opt => opt.MapFrom(src => src.Amount))
            .ForMember(x => x.Id, opt => opt.Ignore())
            .ForMember(x => x.AmountPayed, opt => opt.Ignore())
            .ForMember(x => x.Confirmed, opt => opt.Ignore())
            .ForMember(x => x.Created, opt => opt.Ignore())
            .ForMember(x => x.Updated, opt => opt.Ignore());

        CreateMap<Loan, LoanDto>()
            .ForMember(x => x.Amount, opt => opt.MapFrom(src => src.AmountLoaned));

        CreateMap<LoanUpdate.Loan, Domain.Loan>()
            .ForMember(x => x.Description, opt => opt.Ignore())
            .ForMember(x => x.AmountLoaned, opt => opt.Ignore())
            .ForMember(x => x.AmountPayed, opt => opt.Ignore())
            .ForMember(x => x.Confirmed, opt => opt.Ignore())
            .ForMember(x => x.Id, opt => opt.Ignore())
            .ForMember(x => x.Created, opt => opt.Ignore())
            .ForMember(x => x.Updated, opt => opt.Ignore());

        #endregion
    }
}