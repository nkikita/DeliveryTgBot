using DeliveryTgBot.Interfaces;

namespace DeliveryTgBot.Services
{
    public class ConfigurationService : IConfigurationService
    {
        public string TelegramBotToken { get; }
        public string YandexApiKey { get; }
        public string DatabaseConnectionString { get; }

        public ConfigurationService()
        {
            // In a real application, these would come from environment variables or configuration files
            TelegramBotToken = "7617124159:AAHzbKa64p9Nlx0c6m0u5M_4m0P1NDtAMbA";
            YandexApiKey = "c94023be-a9fe-4530-b47e-7ee3296a33b8";
            DatabaseConnectionString = "Host=localhost;Database=deliverydb;Username=postgres;Password=111";
        }
    }
}
