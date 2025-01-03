using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Tarscord.Core.Persistence;

namespace Tarscord.Core.Features.Loans;

public class UpdateLoanCommand : IRequest<LoanDto>
{
    public decimal Amount { get; set; }
    public ulong LoanedFrom { get; set; }
    public string LoanedFromUsername { get; set; }
    public ulong LoanedTo { get; set; }
    public string LoanedToUsername { get; set; }
}

public class CommandValidator : AbstractValidator<UpdateLoanCommand>
{
    public CommandValidator()
    {
        // RuleFor(x => x.Loan).NotNull();
    }
}

public class UpdateLoanCommandHandler : IRequestHandler<UpdateLoanCommand, LoanDto>
{
    private readonly ILogger<CreateLoanCommandHandler> _logger;
    private readonly IDatabaseConnection _databaseConnection;

    public UpdateLoanCommandHandler(ILogger<CreateLoanCommandHandler> logger, IDatabaseConnection databaseConnection)
    {
        _logger = logger;
        _databaseConnection = databaseConnection;
    }

    // public async Task<LoanDto> Handle(UpdateLoanCommandCommand request, CancellationToken cancellationToken)
    // {
    //     var loans = await _loanRepository
    //         .FindBy(x => x.LoanedFrom == request.Loan.LoanedFrom
    //                      && x.LoanedTo == request.Loan.LoanedTo);
    //
    //     var loanToUpdate = loans?.FirstOrDefault();
    //     if (loanToUpdate == null)
    //     {
    //         return new LoanEnvelope(null);
    //     }
    //
    //     loanToUpdate.AmountPayed += request.Loan.Amount;
    //     loanToUpdate.AmountLoaned -= request.Loan.Amount;
    //     var updatedLoan = await _loanRepository.UpdateItem(_mapper.Map<Domain.Loan>(loanToUpdate));
    //
    //     return new LoanDto();
    // }

    public Task<LoanDto> Handle(UpdateLoanCommand request, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
}