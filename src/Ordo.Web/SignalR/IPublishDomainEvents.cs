using System.Threading.Tasks;

namespace Ordo.Web.SignalR
{
    public interface IPublishDomainEvents
    {
        Task Publish(object evnt);
    }
}
