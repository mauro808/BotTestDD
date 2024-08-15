// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.22.0

using Azure;
using CoreBotTestDD.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CoreBotTestDD.Controllers
{
    // This ASP Controller is created to handle a request. Dependency Injection will provide the Adapter and IBot
    // implementation at runtime. Multiple different IBot implementations running at different endpoints can be
    // achieved by specifying a more specific type for the bot constructor argument.
    [Route("api/messages")]
    [ApiController]
    public class BotController : ControllerBase
    {
        private readonly IBotFrameworkHttpAdapter _adapter;
        private readonly IBot _bot;

        public BotController(IBotFrameworkHttpAdapter adapter, IBot bot)
        {
            _adapter = adapter;
            _bot = bot;
        }

        [HttpPost]
        [HttpGet]
        public async Task PostAsync()
        {
            try
            {
                // Delegate the processing of the HTTP POST to the adapter.
                // The adapter will invoke the bot.
                //var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();
                await _adapter.ProcessAsync(Request, Response, _bot);
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error processing request: {ex.Message}");
                // Optionally, you can return a custom error message
                Response.StatusCode = 500;
                await Response.WriteAsync("Internal Server Error");
            }
        }
    }
}
