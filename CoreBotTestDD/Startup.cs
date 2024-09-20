// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.22.0

using Azure.AI.TextAnalytics;
using Azure;
using CoreBotTestDD.Bots;
using CoreBotTestDD.Dialogs;
using CoreBotTestDD.Services;
using CoreBotTestDD.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Bot.Builder.ApplicationInsights;
using Microsoft.Bot.Builder.Integration.ApplicationInsights.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using DonDoctor.ConfigurationProvider.Core;
using Microsoft.Extensions.Logging;
using DonBot.Dialogs;

namespace CoreBotTestDD
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpClient().AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.MaxDepth = HttpHelper.BotMessageSerializerSettings.MaxDepth;
            });
            AppConfiguration appSettings = AppConfiguration.LoadAppSettings();

            // Create the Bot Framework Authentication to be used with the Bot Adapter.
            services.AddSingleton<InactivityMiddleware>();
            services.AddApplicationInsightsTelemetry();
            services.AddSingleton<IBotTelemetryClient, BotTelemetryClient>();
            services.AddSingleton<ITelemetryInitializer, OperationCorrelationTelemetryInitializer>();
            services.AddSingleton<ITelemetryInitializer, TelemetryBotIdInitializer>();
            services.AddSingleton<TelemetryInitializerMiddleware>();
            services.AddSingleton<TelemetryLoggerMiddleware>();
            services.AddSingleton<IBotFrameworkHttpAdapter>(sp =>
            {
                var auth = sp.GetRequiredService<BotFrameworkAuthentication>();
                var logger = sp.GetRequiredService<ILogger<AdapterWithErrorHandler>>();
                var inactivityMiddleware = sp.GetRequiredService<InactivityMiddleware>();
                var conversationState = sp.GetRequiredService<ConversationState>();
                var telemetryInsight = sp.GetRequiredService<TelemetryInitializerMiddleware>();
                return new AdapterWithErrorHandler(auth, logger, inactivityMiddleware, telemetryInsight, conversationState);
            });
            services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();

            // Create the Bot Adapter with error handling enabled.
            services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();


            // Create the storage we'll be using for User and Conversation state. (Memory is great for testing purposes.)
            services.AddSingleton<IStorage, MemoryStorage>();
            services.AddSingleton(sp =>
            {
                var endpoint = ApplicationConfigurationProvider.AppSetting("TextAnalitycsEndpoint");
                var apiKey = ApplicationConfigurationProvider.AppSetting("TextAnalitycsKey");
                var credentials = new AzureKeyCredential(apiKey);
                return new TextAnalyticsClient(new Uri(endpoint), credentials);
            });

            services.AddSingleton(sp =>
            {
                var textAnalyticsClient = sp.GetRequiredService<TextAnalyticsClient>();
                var CluClient = sp.GetRequiredService<CLUService>();
                return new CustomChoicePrompt(nameof(CustomChoicePrompt), textAnalyticsClient, CluClient);
            });
            services.AddSingleton(new CLUService(appSettings));
            services.AddSingleton(new CQAService(appSettings));
            services.AddSingleton(new ApiCalls());
            services.AddSingleton(new ClientMessages(new ApiCalls()));
            
            // Create the User state. (Used in this bot's Dialog implementation.)
            services.AddSingleton<UserState>();

            // Create the Conversation state. (Used by the Dialog system itself.)
            services.AddSingleton<ConversationState>();

            // Register the BookingDialog.
            services.AddSingleton<BookingDialog>();

            // The MainDialog that will be run by the bot.
            services.AddSingleton<MainDialog>();
            services.AddTransient<AgendarDialog>();
            services.AddSingleton<RegisterDialog>();
            services.AddSingleton<ListaEsperaDialog>();
            services.AddSingleton<ReagendamientoDialog>();
            services.AddSingleton<ScheduleAvalaibilityByMonth>();
            services.AddSingleton<AgendarByDoctorDialog>();
            services.AddSingleton<CancelarCitaDialog>();
            //services.AddHostedService<InactivityBackgroundService>();
            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            services.AddTransient<IBot, DialogAndWelcomeBot<MainDialog>>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseWebSockets()
                .UseRouting()
                .UseAuthorization()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });

            // app.UseHttpsRedirection();
        }
    }
}
