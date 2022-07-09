

namespace ZooKeeper.Client.Implementation
{
    public class ZookeeperClientFactory : IZookeeperClientFactory
    {
        public ZookeeperClient Get(ZookeeperRegistryConfiguration config)
        {
            return new ZookeeperClient($"{config.Host}:{config.Port}");
        }
    }
}