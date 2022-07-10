using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using ZooKeeper.Client;
using ZooKeeper.Client.Implementation;

namespace Ocelot.Client.ZooKeeper
{
    public static class ServiceCollectionExtensions
    {
        private static ZookeeperRegistryConfiguration _zookeeperRegistryConfiguration = new ZookeeperRegistryConfiguration();

        /// <summary>
        /// 添加Ocelot Zookeeper 客户端
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddOcelotZookeeperClient(this IServiceCollection services)
        {
            services.AddSingleton<ZookeeperRegistryConfiguration>(_zookeeperRegistryConfiguration);
            IConfiguration configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
            var configurationSection = configuration.GetSection("Zookeeper");
            configurationSection.Bind(_zookeeperRegistryConfiguration);
            configurationSection.GetReloadToken().RegisterChangeCallback(o => { configurationSection.Bind(_zookeeperRegistryConfiguration); }, null);
            services.AddSingleton<IZookeeperClientFactory, ZookeeperClientFactory>();
            services.AddSingleton<IOcelotClient, ZookeeperOcelotClient>();
            services.AddSingleton<IHostedService, LifetimeEventsHostedService>();
            return services;
        }

        /// <summary>
        /// 添加Ocelot Zookeeper 客户端
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddOcelotZookeeperClient(this IServiceCollection services, Action<ZookeeperRegistryConfiguration> setupConfig)
        {
            services.AddSingleton(_zookeeperRegistryConfiguration);
            setupConfig(_zookeeperRegistryConfiguration);
            services.AddSingleton<IZookeeperClientFactory, ZookeeperClientFactory>();
            services.AddSingleton<IOcelotClient, ZookeeperOcelotClient>();
            services.AddSingleton<IHostedService, LifetimeEventsHostedService>();
            return services;
        }
    }
}