using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Tarscord.Core.Persistence;

namespace Tarscord.Core.Features.Loans;

public record GetLoansQuery : IRequest<IEnumerable<LoanEnvelope>>;

public class GetLoansQueryHandler : IRequestHandler<GetLoansQuery, IEnumerable<LoanEnvelope>>
{
    private readonly ILogger<GetLoansQueryHandler> _logger;

    public GetLoansQueryHandler(
        ILogger<GetLoansQueryHandler> logger)
    {
        _logger = logger;
    }

    public Task<IEnumerable<LoanEnvelope>> Handle(GetLoansQuery request, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
}