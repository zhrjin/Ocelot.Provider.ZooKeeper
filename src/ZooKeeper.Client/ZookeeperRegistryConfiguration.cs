namespace ZooKeeper.Client
{
    public class ZookeeperRegistryConfiguration
    {
        public ZookeeperRegistryConfiguration()
        {
            this.Host = "localhost";
            this.Port = 2181;
        }

        public ZookeeperRegistryConfiguration(string host, int port, string keyOfServiceInZookeeper)
        {
            this.Host = string.IsNullOrEmpty(host) ? "localhost" : host;
            this.Port = port > 0 ? port : 2181;
            this.KeyOfServiceInZookeeper = keyOfServiceInZookeeper;

            // this.Token = token;
        }

        public string KeyOfServiceInZookeeper { get; set; }

        public string Host { get; set; }

        public int Port { get; set; }

        // public string Token { get; }
    }
}