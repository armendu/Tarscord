using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using MediatR;
using Tarscord.Persistence.Interfaces;

namespace Tarscord.Core.Features.Loans
{
    public class Create
    {
        public class Loan
        {
        }

        public class Command : IRequest<LoanEnvelope>
        {
            public Loan Loan { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.Loan).NotNull();
            }
        }

        public class Handler : IRequestHandler<Command, LoanEnvelope>
        {
            private readonly ILoanRepository _loanRepository;
            private readonly IMapper _mapper;

            public Handler(ILoanRepository loanRepository, IMapper mapper)
            {
                _loanRepository = loanRepository;
                _mapper = mapper;
            }

            public async Task<LoanEnvelope> Handle(Command message, CancellationToken cancellationToken)
            {
                var createdLoan = await _loanRepository.InsertAsync(_mapper.Map<Domain.Loan>(message.Loan))
                    .ConfigureAwait(false);

                return new LoanEnvelope(createdLoan);
            }
        }
    }
}