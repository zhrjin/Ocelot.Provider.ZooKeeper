using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using ZooKeeper.Client;
using ZooKeeper.Client.Implementation;

namespace Ocelot.Provider.ZooKeeper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Infrastructure.Extensions;
    using Newtonsoft.Json;
    using ServiceDiscovery.Providers;
    using Values;

    public class Zookeeper : IServiceDiscoveryProvider
    {
        private readonly ZookeeperRegistryConfiguration _config;
        private readonly ILogger _logger;
        private readonly ZookeeperClient _zookeeperClient;
        private const string VersionPrefix = "version-";
        private readonly ConcurrentDictionary<string, List<Service>> _serviceDic;

        public Zookeeper(ZookeeperRegistryConfiguration config, ILoggerFactory factory, IZookeeperClientFactory clientFactory)
        {
            _logger = factory.CreateLogger<Zookeeper>();
            _config = config;
            _zookeeperClient = clientFactory.Get(_config);
            _serviceDic = new ConcurrentDictionary<string, List<Service>>();
            _zookeeperClient.SubscribeChildrenChange(ZookeeperKey, Listener);
        }

        public string ZookeeperKey => $"/Ocelot/Services/{_config.KeyOfServiceInZookeeper}";

        private async Task Listener(IZookeeperClient client, NodeChildrenChangeArgs args)
        {
            _logger.LogInformation("node changed. eventType={0} CurrentChildrens={1}", args.Type.ToString(), args.CurrentChildrens);
            var queryResult = await client.GetRangeAsync(args.Path, args.CurrentChildrens);
            var services = new List<Service>();
            foreach (var dic in queryResult)
            {
                var serviceEntry = JsonConvert.DeserializeObject<ServiceEntry>(dic.Value);
                if (IsValid(serviceEntry))
                {
                    services.Add(BuildService(serviceEntry));
                }
            }

            _serviceDic[_config.KeyOfServiceInZookeeper] = services;
        }

        public async Task<List<Service>> Get()
        {
            bool hasValue = _serviceDic.TryGetValue(_config.KeyOfServiceInZookeeper, out var value);
            if (!hasValue)
            {
                _logger.LogInformation("read zookeeper data");

                // Services/srvname/srvid
                var queryResult = await _zookeeperClient.GetRangeAsync(ZookeeperKey);
                var services = new List<Service>();
                foreach (var dic in queryResult)
                {
                    var serviceEntry = JsonConvert.DeserializeObject<ServiceEntry>(dic.Value);
                    if (IsValid(serviceEntry))
                    {
                        services.Add(BuildService(serviceEntry));
                    }
                    else
                    {
                        _logger.LogWarning($"Unable to use service Address: {serviceEntry.Host} and Port: {serviceEntry.Port} as it is invalid. Address must contain host only e.g. localhost and port must be greater than 0");
                    }
                }

                value = services;
                _serviceDic.TryAdd(_config.KeyOfServiceInZookeeper, value);
            }

            return value;
        }

        public static Service BuildService(ServiceEntry serviceEntry)
        {
            return new Service(
                serviceEntry.Name,
                new ServiceHostAndPort(serviceEntry.Host, serviceEntry.Port),
                serviceEntry.Id,
                string.IsNullOrWhiteSpace(serviceEntry.Version) ? GetVersionFromStrings(serviceEntry.Tags) : serviceEntry.Version,
                serviceEntry.Tags ?? Enumerable.Empty<string>());
        }

        public static bool IsValid(ServiceEntry serviceEntry)
        {
            if (string.IsNullOrEmpty(serviceEntry.Host) || serviceEntry.Host.Contains("http://") || serviceEntry.Host.Contains("https://") || serviceEntry.Port <= 0)
            {
                return false;
            }

            return true;
        }

        public static string GetVersionFromStrings(IEnumerable<string> strings)
        {
            return strings
                ?.FirstOrDefault(x => x.StartsWith(VersionPrefix, StringComparison.Ordinal))
                .TrimStart(VersionPrefix);
        }
    }
}