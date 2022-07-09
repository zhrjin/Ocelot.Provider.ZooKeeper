using System.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ZooKeeper.Client;
using ZooKeeper.Client.Implementation;

namespace Ocelot.Provider.ZooKeeper
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using ServiceDiscovery.Providers;
    using Values;

    public class PollZookeeper : IServiceDiscoveryProvider
    {
        private readonly ZookeeperRegistryConfiguration _config;
        private readonly ZookeeperClient _zookeeperClient;
        private readonly ILogger _logger;
        private readonly Timer _timer;
        private bool _polling;
        private List<Service> _services;

        public PollZookeeper(int pollingInterval, ILoggerFactory factory, IZookeeperClientFactory clientFactory, ZookeeperRegistryConfiguration config)
        {
            _config = config;
            _zookeeperClient = clientFactory.Get(config);
            _logger = factory.CreateLogger<PollZookeeper>();
            _services = new List<Service>();

            _timer = new Timer(
                async x =>
            {
                if (_polling)
                {
                    return;
                }

                _polling = true;
                await Poll();
                _polling = false;
            }, null, pollingInterval, pollingInterval);
        }

        public Task<List<Service>> Get()
        {
            return Task.FromResult(_services);
        }

        private async Task Poll()
        {
            // Services/srvname/srvid
            var queryResult = await _zookeeperClient.GetRangeAsync($"/Ocelot/Services/{_config.KeyOfServiceInZookeeper}");

            var services = new List<Service>();

            foreach (var dic in queryResult)
            {
                var serviceEntry = JsonConvert.DeserializeObject<ServiceEntry>(dic.Value);
                if (Zookeeper.IsValid(serviceEntry))
                {
                    services.Add(Zookeeper.BuildService(serviceEntry));
                }
                else
                {
                    _logger.LogWarning(
                        $"Unable to use service Address: {serviceEntry.Host} and Port: {serviceEntry.Port} as it is invalid. Address must contain host only e.g. localhost and port must be greater than 0");
                }
            }

            _services = services.ToList();
        }
    }
}