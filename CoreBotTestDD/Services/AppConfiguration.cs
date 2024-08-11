using Microsoft.Extensions.Configuration;

namespace CoreBotTestDD.Services
{
    public class AppConfiguration
    {
        public string ApiKey { get; set; }
        public string Endpoint { get; set; }
        public string ServiceUrl { get; set; }
        public string MasterRobotUrl { get; set; }  

        public static AppConfiguration LoadAppSettings()
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            var appSettings = new AppConfiguration();
            configuration.GetSection("AppSettings").Bind(appSettings);
            configuration.GetSection("CLUSettings").Bind(appSettings);
            configuration.GetSection("CustomQuestionAnswering").Bind(appSettings);

            return appSettings;
        }

    }
}
