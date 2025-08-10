namespace DeliveryTgbot.Models;

public class Driver
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? Name { get; set; }
    public int CityId { get; set; } // внешний ключ на таблицу City
    [JsonIgnore] 
    public Cityes City { get; set; }
    public decimal PricePerVolume { get; set; } // Цена за объем
    public double MaxVolume { get; set; } // Максимальный объем
    public string? Contact { get; set; }
    public long TelegramId { get; set; } // chatId
    public bool IsAvailable { get; set; } = true;
}