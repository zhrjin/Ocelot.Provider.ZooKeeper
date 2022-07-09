using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace Ocelot.Client.ZooKeeper
{
    public interface IOcelotClient
    {
        public void Init(IServerAddressesFeature serverAddressesFeature);
        public Task RegisterService();
        
        public Task UnRegisterService();
    }
}