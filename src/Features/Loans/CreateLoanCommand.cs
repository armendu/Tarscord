using System.Threading;
using System.Threading.Tasks;
using Dapper.Contrib.Extensions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Tarscord.Core.Persistence;

namespace Tarscord.Core.Features.Loans;

public class CreateLoanCommand : IRequest<LoanDto>
{
    public decimal Amount { get; set; }
    public ulong LoanedFrom { get; set; }
    public string LoanedFromUsername { get; set; }
    public ulong LoanedTo { get; set; }
    public string LoanedToUsername { get; set; }
    public string Description { get; set; }
}

public class CreateLoanCommandValidator : AbstractValidator<CreateLoanCommand>
{
    public CreateLoanCommandValidator()
    {
        // TODO: Add proper validation
        // RuleFor(x => x.Loan).NotNull();
    }
}

public class CreateLoanCommandHandler : IRequestHandler<CreateLoanCommand, LoanDto>
{
    private readonly ILogger<CreateLoanCommandHandler> _logger;
    private readonly IDatabaseConnection _databaseConnection;

    public CreateLoanCommandHandler(
        ILogger<CreateLoanCommandHandler> logger,
        IDatabaseConnection databaseConnection)
    {
        _logger = logger;
        _databaseConnection = databaseConnection;
    }

    public async Task<LoanDto> Handle(CreateLoanCommand command, CancellationToken cancellationToken)
    {
        var createdLoan = await _databaseConnection.Connection.InsertAsync(command)
            .ConfigureAwait(false);

        return new LoanDto();
    }
}