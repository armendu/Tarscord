using Tarscord.Core.Domain;
using Tarscord.Core.Persistence.Interfaces;

namespace Tarscord.Core.Persistence.Repositories;

public class LoanRepository : BaseRepository<Loan>, ILoanRepository
{
    public LoanRepository(IDatabaseConnection connection)
        : base(connection)
    {
    }
}