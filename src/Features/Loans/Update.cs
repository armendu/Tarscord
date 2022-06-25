using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using MediatR;
using Tarscord.Persistence.Interfaces;

namespace Tarscord.Core.Features.Loans
{
    public class Update
    {
        public class Loan
        {
            public decimal Amount { get; set; }
            public ulong LoanedFrom { get; set; }
            public string LoanedFromUsername { get; set; }
            public ulong LoanedTo { get; set; }
            public string LoanedToUsername { get; set; }
        }

        public class Command : IRequest<LoanEnvelope>
        {
            public Loan Loan { get; init; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.Loan).NotNull();
            }
        }

        public class CommandHandler : IRequestHandler<Command, LoanEnvelope>
        {
            private readonly ILoanRepository _loanRepository;
            private readonly IMapper _mapper;

            public CommandHandler(ILoanRepository loanRepository, IMapper mapper)
            {
                _loanRepository = loanRepository;
                _mapper = mapper;
            }

            public async Task<LoanEnvelope> Handle(Command request, CancellationToken cancellationToken)
            {
                var loans = await _loanRepository
                    .FindBy(x => x.LoanedFrom == request.Loan.LoanedFrom
                                 && x.LoanedTo == request.Loan.LoanedTo);

                var loanToUpdate = loans?.FirstOrDefault();
                if (loanToUpdate == null)
                {
                    return new LoanEnvelope(null);
                }

                loanToUpdate.AmountPayed += request.Loan.Amount;
                loanToUpdate.AmountLoaned -= request.Loan.Amount;
                var updatedLoan = await _loanRepository.UpdateItem(_mapper.Map<Domain.Loan>(loanToUpdate));

                return new LoanEnvelope(updatedLoan);
            }
        }
    }
}