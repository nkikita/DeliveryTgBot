namespace DeliveryTgBot.Interfaces
{
    public interface ICallbackHandler
    {
        bool CanHandle(string callbackData);
        Task HandleAsync(long chatId, string callbackData);
    }
}
