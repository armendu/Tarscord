using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Tarscord.Core.Persistence.Interfaces;

namespace Tarscord.Core.Features.Loans;

public class List
{
    public record Query : IRequest<LoanListEnvelope>;

    public class QueryHandler : IRequestHandler<Query, LoanListEnvelope>
    {
        private readonly ILoanRepository _loanRepository;

        public QueryHandler(ILoanRepository loanRepository)
        {
            _loanRepository = loanRepository;
        }

        public async Task<LoanListEnvelope> Handle(Query message, CancellationToken cancellationToken)
        {
            var loans =
                await _loanRepository.GetAllAsync().ConfigureAwait(false);

            return new LoanListEnvelope(loans.ToList());
        }
    }
}