using System.Collections.Generic;
using Tarscord.Core.Domain;

namespace Tarscord.Core.Features.Loans;

public record LoanListEnvelope(IList<Loan> Loans);