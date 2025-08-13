namespace DeliveryTgBot.Interfaces
{
    public interface IOrderStateManager
    {
        Task<bool> ProcessOrderStateAsync(Order order, string input);
        Task<bool> IsOrderCompleteAsync(Order order);
        Task<string> GetNextPromptAsync(Order order);
    }
}
