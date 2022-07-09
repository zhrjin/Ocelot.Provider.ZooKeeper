using ZooKeeper.Client.Implementation;

namespace ZooKeeper.Client
{
    public interface IZookeeperClientFactory
    {
        ZookeeperClient Get(ZookeeperRegistryConfiguration config);
    }
}