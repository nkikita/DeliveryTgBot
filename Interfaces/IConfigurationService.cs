namespace DeliveryTgBot.Interfaces
{
    public interface IConfigurationService
    {
        string TelegramBotToken { get; }
        string YandexApiKey { get; }
        string DatabaseConnectionString { get; }
    }
}
