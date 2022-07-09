﻿using System.Collections.Generic;

namespace Ocelot.Client.ZooKeeper
{
    public class ServiceEntry
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public IEnumerable<string> Tags { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
    }
}