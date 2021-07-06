using Tarscord.Core.Domain;
using Tarscord.Persistence.Interfaces;

namespace Tarscord.Persistence.Repositories
{
    public class LoanRepository : BaseRepository<Loan>, ILoanRepository
    {
        public LoanRepository(IDatabaseConnection connection)
            : base(connection)
        {
        }
    }
}