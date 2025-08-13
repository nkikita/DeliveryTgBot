namespace DeliveryTgBot.Interfaces
{
    public interface ICommandHandler
    {
        string Command { get; }
        Task HandleAsync(long chatId, string[] arguments);
    }
}
