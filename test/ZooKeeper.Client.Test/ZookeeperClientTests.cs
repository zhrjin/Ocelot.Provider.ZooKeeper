﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using org.apache.zookeeper;
using ZooKeeper.Client.Implementation;

namespace ZooKeeper.Client.Test
{
    public class ZookeeperClientTests
    {
        private IZookeeperClient _client;
        
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
           _client = new ZookeeperClientFactory().Get(new ZookeeperRegistryConfiguration("127.0.0.1", 2181, nameof(ZookeeperClientTests)));
        }

        [Test]
        public async Task GetChildrenAsyncTest()
        {
            var childrens = await _client.GetChildrenAsync("/");
            Console.WriteLine(JsonSerializer.Serialize(childrens));
            childrens = await _client.GetChildrenAsync("/zookeeper");
            Console.WriteLine(JsonSerializer.Serialize(childrens));
        }

        [Test]
        public async Task ExistsAsyncTest()
        {
            var result = await _client.ExistsAsync("/");
            Assert.IsTrue(result);
        }

        [Test]
        public async Task GetDataAsyncTest()
        {
            var data = await _client.GetDataAsync("/");

            //data = await _client.GetDataAsync("/chanelInfo");
        }

        [Test]
        public async Task ReconnectionTest()
        {
            await _client.ExistsAsync("/");
            await Task.Delay(TimeSpan.FromSeconds(8));
            await _client.ExistsAsync("/");
        }

        [Test]
        public async Task CreateTest()
        {
            var path = $"/{Guid.NewGuid():N}";

            if (await _client.ExistsAsync(path))
                await _client.DeleteAsync(path);

            await _client.CreateEphemeralAsync(path, Encoding.UTF8.GetBytes("abc"));

            var data = (await _client.GetDataAsync(path)).ToArray();

            await _client.DeleteAsync(path);
        }

        [Test]
        public async Task DeleteTest()
        {
            var path = $"/{Guid.NewGuid():N}";

            if (await _client.ExistsAsync(path))
            {
                await _client.DeleteAsync(path);
            }
            else
            {
                await _client.CreateEphemeralAsync(path, null);
                await Task.Delay(1000);
                if (await _client.ExistsAsync(path))
                    await _client.DeleteAsync(path);
            }
            var sd = await _client.ExistsAsync(path);
        }

        [Test]
        public async Task SubscribeDataChangeTest()
        {
            var path = $"/{DateTime.Now:yyyy_MM_dd_HH_mm_ss_ff}";
            try
            {
                if (await _client.ExistsAsync(path))
                    await _client.DeleteAsync(path);

                var types = new List<Watcher.Event.EventType>();
                var waitEvent = new AutoResetEvent(false);

                await _client.SubscribeDataChange(path, (client, args) =>
                {
                    types.Add(args.Type);
                    waitEvent.Set();
                    return Task.CompletedTask;
                });

                //created
                await _client.CreateEphemeralAsync(path, null);
                waitEvent.WaitOne(10000);

                //Assert.Equal(Watcher.Event.EventType.NodeCreated, types[0]);

                //modify
                await _client.SetDataAsync(path, new byte[] { 1 });
                waitEvent.WaitOne(10000);
                // Assert.Equal(Watcher.Event.EventType.NodeDataChanged, types[1]);

                //deleted
                await _client.DeleteAsync(path);
                waitEvent.WaitOne(10000);
                //  Assert.Equal(Watcher.Event.EventType.NodeDeleted, types[2]);
            }
            finally
            {
                if (await _client.ExistsAsync(path))
                    await _client.DeleteAsync(path);
            }
        }

        [Test]
        public async Task SubscribeChildrenChangeTest()
        {
            var path = $"/{DateTime.Now:yyyy_MM_dd_HH_mm_ss_ff}";
            var path2 = $"{path}/123";
            try
            {
                if (await _client.ExistsAsync(path))
                    await _client.DeleteRecursiveAsync(path);

                var types = new List<Watcher.Event.EventType>();

                var semaphore = new Semaphore(0, 2);

                await _client.SubscribeDataChange(path, (client, args) =>
                {
                    if (args.Type == Watcher.Event.EventType.NodeCreated)
                        semaphore.Release();
                    return Task.CompletedTask;
                });
                await _client.SubscribeChildrenChange(path, (client, args) =>
                {
                    types.Add(args.Type);
                    semaphore.Release();
                    return Task.CompletedTask;
                });

                await _client.CreatePersistentAsync(path, null);
                semaphore.WaitOne(10000);
                await _client.CreatePersistentAsync(path2, null);
                semaphore.WaitOne(10000);
                // Assert.Equal(Watcher.Event.EventType.NodeChildrenChanged, types[0]);
            }
            finally
            {
                if (await _client.ExistsAsync(path))
                    await _client.DeleteRecursiveAsync(path);
            }
        }

        /*        [Fact]
                public async Task ReconnectionDataChangeTest()
                {
                    var path = $"/{DateTime.Now:yyyy_MM_dd_HH_mm_ss_ff}";

                    if (await _client.ExistsAsync(path))
                        await _client.DeleteRecursiveAsync(path);

                    await _client.CreateEphemeralAsync(path, null);

                    var isChange = false;

                    await _client.SubscribeDataChange(path, (client, args) =>
                    {
                        if (args.Type == NodeListenerType.DataChanged)
                            isChange = true;
                        return Task.CompletedTask;
                    });

                    await Task.Delay(TimeSpan.FromSeconds(15));

                    Assert.True(isChange);
                }*/

        public async Task UnSubscribeTest()
        {
            var path = $"/{DateTime.Now:yyyy_MM_dd_HH_mm_ss_ff}";

            var count = 0;

            var waitEvent = new AutoResetEvent(false);
            NodeDataChangeHandler handler = (client, args) =>
            {
                count++;
                waitEvent.Set();
                return Task.CompletedTask;
            };

            await _client.SubscribeDataChange(path, handler);

            await _client.CreateEphemeralAsync(path, null);

            waitEvent.WaitOne(10000);
            // Assert.Equal(1, count);

            _client.UnSubscribeDataChange(path, handler);

            await _client.DeleteAsync(path);

            // Assert.Equal(1, count);
        }

        //
        public async Task CreateRecursiveAndDeleteRecursiveTest()
        {
            var pathRoot = $"/{DateTime.Now:yyyy_MM_dd_HH_mm_ss_ff}";
            var path = $"{pathRoot}/1/2";
            if (await _client.ExistsAsync(pathRoot))
                await _client.DeleteRecursiveAsync(pathRoot);

            await _client.CreateRecursiveAsync(path, null);
            //  Assert.True(await _client.ExistsAsync(path));

            await _client.DeleteRecursiveAsync(pathRoot);
            if (await _client.ExistsAsync(pathRoot))
                throw new Exception("删除失败");
        }
    }
}