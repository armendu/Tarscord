using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Tarscord.Core.Persistence;

namespace Tarscord.Core.Features.Loans;

public record GetLoansQuery : IRequest<IEnumerable<LoanDto>>;

public class GetLoansQueryHandler : IRequestHandler<GetLoansQuery, IEnumerable<LoanDto>>
{
    private readonly ILogger<GetLoansQueryHandler> _logger;

    public GetLoansQueryHandler(
        ILogger<GetLoansQueryHandler> logger)
    {
        _logger = logger;
    }

    public Task<IEnumerable<LoanDto>> Handle(GetLoansQuery request, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
}