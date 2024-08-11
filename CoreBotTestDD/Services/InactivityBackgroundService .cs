using CoreBotTestDD.Bots;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Bot.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace CoreBotTestDD.Services
{
    public class InactivityBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);
        private readonly TimeSpan _inactivityThreshold = TimeSpan.FromMinutes(10);

        public InactivityBackgroundService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(_checkInterval, stoppingToken);

                using (var scope = _serviceProvider.CreateScope())
                {
                    var inactivityMiddleware = scope.ServiceProvider.GetRequiredService<InactivityMiddleware>();
                    await inactivityMiddleware.CheckInactivityAsync(_inactivityThreshold, stoppingToken);
                }
            }
        }
    }

}
