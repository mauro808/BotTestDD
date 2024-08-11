using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.AspNetCore.Identity;
using CoreBotTestDD.Models;

namespace CoreBotTestDD.Bots
{
    public class InactivityMiddleware : IMiddleware
    {
        private DateTime _lastActivity;

        public InactivityMiddleware()
        {
            _lastActivity = DateTime.UtcNow;
        }

        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default)
        {
            _lastActivity = DateTime.UtcNow;
            await next(cancellationToken);
        }

        public Task CheckInactivityAsync(TimeSpan inactivityThreshold, CancellationToken stoppingToken)
        {
             var now = DateTime.UtcNow;
            if (now - _lastActivity > inactivityThreshold)
            {
                // Realizar acciones por inactividad
                HandleInactiveUser();
            }

            return Task.CompletedTask;
        }

        private void HandleInactiveUser()
        {
            // Implementar acciones por inactividad, como enviar un mensaje o cerrar sesión
            Console.WriteLine("Usuario inactivo, realizar acciones necesarias.");
        }
    }
}
