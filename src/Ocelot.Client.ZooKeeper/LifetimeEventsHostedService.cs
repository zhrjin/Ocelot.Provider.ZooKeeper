using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ocelot.Client.ZooKeeper
{
    public class LifetimeEventsHostedService : IHostedService
    {
        private readonly ILogger _logger;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly IOcelotClient _ocelotClient;

        public LifetimeEventsHostedService(ILoggerFactory loggerFactory, IHostApplicationLifetime appLifetime, IOcelotClient ocelotClient)
        {
            _logger = loggerFactory.CreateLogger(GetType());
            _appLifetime = appLifetime;
            _ocelotClient = ocelotClient;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _appLifetime.ApplicationStarted.Register(OnStarted);
            _appLifetime.ApplicationStopping.Register(OnStopping);
            //_appLifetime.ApplicationStopped.Register(OnStopped);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private void OnStarted()
        {
            // Perform post-startup activities here
            _ocelotClient.RegisterService();
        }

        private void OnStopping()
        {
            // Perform on-stopping activities here
            _ocelotClient.UnRegisterService();
        }

        private void OnStopped()
        {
            // Perform post-stopped activities here
        }
    }
}