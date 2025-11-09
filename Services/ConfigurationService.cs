using DeliveryTgBot.Interfaces;
using Microsoft.Extensions.Configuration;

namespace DeliveryTgBot.Services
{
    public class ConfigurationService : IConfigurationService
    {
        public string TelegramBotToken { get; }
        public string YandexApiKey { get; }
        public string DatabaseConnectionString { get; }
        public long ManagerTelegramUserId { get; }

        public ConfigurationService(IConfiguration configuration)
        {
            // Prefer values from appsettings/environment; fallback to existing defaults
            TelegramBotToken = configuration["Telegram:BotToken"] ?? "";
            YandexApiKey = configuration["Yandex:ApiKey"] ?? "";
            DatabaseConnectionString = configuration.GetConnectionString("Default") ?? "Host=localhost;Database=deliverydb;Username=postgres;Password=111";

            var managerIdStr = configuration["Manager:TelegramUserId"];
            if (!long.TryParse(managerIdStr, out var managerId))
            {
                managerId = 0;
            }
            ManagerTelegramUserId = managerId;
        }
    }
}
