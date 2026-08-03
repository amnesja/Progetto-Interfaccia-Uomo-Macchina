using Ordo.Services.Shared;
using Microsoft.Extensions.DependencyInjection;
using Ordo.Web.SignalR;

namespace Ordo.Web
{
    public class Container
    {
        public static void RegisterTypes(IServiceCollection container)
        {
            // Registration of all the database services you have
            container.AddScoped<SharedService>();

            // Registration of SignalR events
            container.AddScoped<IPublishDomainEvents, SignalrPublishDomainEvents>();
        }
    }
}
