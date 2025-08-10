namespace DeliveryTgbot.Models;

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public long ClientTelegramId { get; set; }
    public int? CityId { get; set; }
    [JsonIgnore]
    public Cityes City { get; set; }
    public double Volume { get; set; }
    public int VehiclesCount { get; set; }
    public DateTime DeliveryDateTime { get; set; }
    public Guid? AssignedDriverId { get; set; }
    [JsonIgnore]
    public OrderStatus Status { get; set; } = OrderStatus.Created;
    public string? DriverResponseReason { get; set; }
    public string? DeliveryAdress { get; set; } 

    public string? CommentFromUsers { get; set; }
    
}