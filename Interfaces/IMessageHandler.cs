namespace DeliveryTgBot.Interfaces
{
    public interface IMessageHandler
    {
        bool CanHandle(Update update);
        Task HandleAsync(Update update);
    }
}
