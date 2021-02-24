using AutoMapper;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tarscord.Persistence.Interfaces;

namespace Tarscord.Application.Services.Services
{
    public class LoanService
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

        public async Task GetAllEvents()
        {
        }
    }
}