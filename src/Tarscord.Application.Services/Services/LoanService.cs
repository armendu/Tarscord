using AutoMapper;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tarscord.Application.Services.Interfaces;
using Tarscord.Common.Models;
using Tarscord.Persistence.Entities;
using Tarscord.Persistence.Interfaces;

namespace Tarscord.Application.Services.Services
{
    public class LoanService : ILoanService
    {
        private readonly ILoanRepository _loanRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<EventService> _logger;

        public LoanService(
            ILoanRepository loanRepository,
            IMapper mapper,
            ILogger<EventService> logger)
        {
            _loanRepository = loanRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<LoanInfo>> GetAllLoans(bool isConfirmed = true)
        {
            var result = await _loanRepository.GetAllAsync();

            return result.Select(loan => _mapper.Map<LoanInfo>(loan));
        }
    }
}