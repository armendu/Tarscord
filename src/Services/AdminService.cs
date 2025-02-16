using Tarscord.Core.Persistence.Interfaces;

namespace Tarscord.Core.Services;

public class AdminService(IUserRepository userRepository)
{
    private readonly IUserRepository _userRepository = userRepository;

    // public async Task MuteUser(string id, int minutesToMute)
    // {
    //     var userToMute =
    //         (await _userRepository.FindBy(u => u.Id == id).ConfigureAwait(false)).FirstOrDefault();
    // }
}