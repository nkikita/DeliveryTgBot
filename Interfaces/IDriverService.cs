namespace DeliveryTgBot.Interfaces
{
    public interface IDriverService
    {
        Task<IEnumerable<Driver>> GetAvailableDriversAsync(int city, double volume, int vehiclesCount);
        Task<Driver> GetDriverByIdAsync(Guid driverId);
        Task UpdateDriverStatusAsync(Guid driverId, bool isAvailable);
    }
}