using Microsoft.Extensions.Logging;
using ZooKeeper.Client;

namespace Ocelot.Provider.ZooKeeper
{
    using Microsoft.Extensions.DependencyInjection;
    using ServiceDiscovery;

    public static class ZookeeperProviderFactory
    {
        public static ServiceDiscoveryFinderDelegate Get = (provider, config, route) =>
        {
            var factory = provider.GetService<ILoggerFactory>();

            var ZookeeperFactory = provider.GetService<IZookeeperClientFactory>();

            var ZookeeperRegistryConfiguration = new ZookeeperRegistryConfiguration(config.Host, config.Port, route.ServiceName);

            var ZookeeperServiceDiscoveryProvider = new Zookeeper(ZookeeperRegistryConfiguration, factory, ZookeeperFactory);

            if (config.Type?.ToLower() == "pollZookeeper")
            {
                return new PollZookeeper(config.PollingInterval, factory, ZookeeperFactory, ZookeeperRegistryConfiguration);
            }

            return ZookeeperServiceDiscoveryProvider;
        };
    }
}