using System.Collections.Generic;
using System.Threading.Tasks;
using Tarscord.Common.Models;

namespace Tarscord.Application.Services.Interfaces
{
    public interface ILoanService
    {
        Task<IEnumerable<LoanInfo>> GetAllLoans(bool isConfirmed = true);
    }
}