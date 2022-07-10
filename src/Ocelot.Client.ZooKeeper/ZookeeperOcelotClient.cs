using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ZooKeeper.Client;
using ZooKeeper.Client.Implementation;

namespace Ocelot.Client.ZooKeeper
{
    public class ZookeeperOcelotClient : IOcelotClient
    {
        private readonly ZookeeperRegistryConfiguration _config;
        private readonly IWebHostEnvironment _hostEnvironment;  
        private readonly ILogger _logger;
        private readonly ZookeeperClient _zookeeperClient;
        private const string VersionPrefix = "version-";

        public ZookeeperOcelotClient(ZookeeperRegistryConfiguration config, ILoggerFactory factory, IZookeeperClientFactory clientFactory,
            IWebHostEnvironment hostEnvironment)
        {
            _logger = factory.CreateLogger<ZookeeperOcelotClient>();
            _config = config;
            _hostEnvironment = hostEnvironment;
            _zookeeperClient = clientFactory.Get(_config);
        }

        public IServerAddressesFeature ServerAddressesFeature { get; private set; }
        public ServiceEntry ServiceEntry { get; protected set; }
        public string ServicePath { get; private set; }

        public void Init(IServerAddressesFeature serverAddressesFeature)
        {
            this.ServerAddressesFeature = serverAddressesFeature;
        }

        public async Task RegisterService()
        {
            try
            {
                BuildServiceEntry();

                // Services/srvname/srvid
                string path = $"/Ocelot/Services/{ServiceEntry.Name}";
                if (!await _zookeeperClient.ExistsAsync(path))
                {
                    await _zookeeperClient.CreateRecursiveAsync(path, Array.Empty<byte>());
                }

                bool exist = await _zookeeperClient.ExistsAsync($"{path}/{ServiceEntry.Id}");
                ServicePath = await _zookeeperClient.CreateEphemeralAsync($"{path}/{ServiceEntry.Id}",
                    JsonSerializer.SerializeToUtf8Bytes(ServiceEntry), exist);
                _logger.LogInformation("Register service={0} to zookeeper={1} result={2}", ServiceEntry.Name, _config.Host, ServicePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Register to zookeeper error:{0}", ex.Message);
                throw;
            }
        }

        public async Task UnRegisterService()
        {
            if (await _zookeeperClient.ExistsAsync(ServicePath))
            {
                await _zookeeperClient.DeleteRecursiveAsync(ServicePath);
            }
        }

        private ServiceEntry BuildServiceEntry()
        {
            ServiceEntry = new ServiceEntry();
            ServiceEntry.Id = Environment.MachineName;
            ServiceEntry.Name = GetServiceName();
            var hostString = GetServiceHost(ServerAddressesFeature);
            ServiceEntry.Host = hostString.Host;
            ServiceEntry.Port = hostString.Port ?? 80;
            ServiceEntry.Version = VersionPrefix + Assembly.GetEntryAssembly().GetName().Version;
            return this.ServiceEntry;
        }

        private string GetServiceName()
        {
            if (!string.IsNullOrWhiteSpace(_config.KeyOfServiceInZookeeper))
            {
                return _config.KeyOfServiceInZookeeper;
            }

            string appName = Environment.GetEnvironmentVariable("AppName") ??
                             Environment.GetEnvironmentVariable("AppId") ??
                             Environment.GetEnvironmentVariable("ASPNETCORE_APPLICATIONNAME") ??
                             Environment.GetEnvironmentVariable("DOTNET_APPLICATIONNAME") ??
                             _hostEnvironment.ApplicationName;
            return appName;
        }

        private HostString GetServiceHost(IServerAddressesFeature serverAddressesFeature)
        {
            var address = serverAddressesFeature.Addresses.FirstOrDefault();
            if (string.IsNullOrEmpty(address))
            {
                return new HostString(GetServerIp(), 5000);
            }
            else
            {
                var serverUrl = new UriBuilder(address);
                if (serverUrl.Host.Equals("[::]") || serverUrl.Host.Equals("0.0.0.0"))
                {
                    return new HostString(GetServerIp(), serverUrl.Port);
                }

                return new HostString(serverUrl.Host, serverUrl.Port);
            }
        }

        private string GetServerIp()
        {
            string localIp = NetworkInterface.GetAllNetworkInterfaces()
                .Select(p => p.GetIPProperties())
                .SelectMany(p => p.UnicastAddresses)
                .FirstOrDefault(p => p.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(p.Address))?.Address.ToString();
            return localIp ?? "127.0.0.1";
        }
    }
}