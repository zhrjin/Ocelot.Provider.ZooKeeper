using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Ocelot.Client.ZooKeeper
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseOcelotZookeeperClient(this IApplicationBuilder app)
        {
            var serverAddressesFeature = app.ServerFeatures.Get<IServerAddressesFeature>();
            var ocelotClient = app.ApplicationServices.GetRequiredService<IOcelotClient>();
            ocelotClient.Init(serverAddressesFeature);

            return app;
        }
    }
}