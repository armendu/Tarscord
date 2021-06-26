namespace Tarscord.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Tarscord.Persistence.Interfaces;

    public class AdminService
    {
        private readonly IUserRepository _userRepository;

        public AdminService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task MuteUser(string id, int minutesToMute)
        {
            var userToMute =
                (await _userRepository.FindBy(u => u.Id == id).ConfigureAwait(false)).FirstOrDefault();
        }
    }
}