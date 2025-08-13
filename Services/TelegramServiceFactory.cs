using DeliveryTgBot.Interfaces;
using Telegram.Bot;

namespace DeliveryTgBot.Services
{
    public class TelegramServiceFactory
    {
        private readonly IConfigurationService _configurationService;

        public TelegramServiceFactory(IConfigurationService configurationService)
        {
            _configurationService = configurationService;
        }

        public ITelegramService Create()
        {
            var botClient = new TelegramBotClient(_configurationService.TelegramBotToken);
            return new TelegramService(botClient);
        }
    }
}
