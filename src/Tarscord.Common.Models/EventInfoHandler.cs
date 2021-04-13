using MediatR;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace Tarscord.Common.Models
{
    public class EventInfoHandler : IRequestHandler<EventInfo, string>
    {
        public Task<string> Handle(EventInfo request, CancellationToken cancellationToken)
        {
            return Task.FromResult("Pong");
        }
    }
}