using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Polling;
using DeliveryTgBot.Data;
using Microsoft.Extensions.Logging; 

// 1. Настраиваем DbContext вручную с опциями
var optionsBuilder = new DbContextOptionsBuilder<DeliveryDbContext>();
optionsBuilder.UseNpgsql("Host=localhost;Database=deliverydb;Username=postgres;Password=111");
optionsBuilder
    .UseNpgsql("Host=localhost;Database=deliverydb;Username=postgres;Password=111")
    .LogTo(Console.WriteLine, LogLevel.Information)   // логируем запросы и ошибки
    .EnableSensitiveDataLogging();    
using var dbContext = new DeliveryDbContext(optionsBuilder.Options);
dbContext.Database.Migrate();

// 2. Создаём сервисы, передавая dbContext
IDriverService driverService = new DriverService(dbContext);
IOrderCacheService orderService = new OrderCacheService();
ICityService cityService = new CityService(dbContext);
IOrderService DBorderService = new OrderService(dbContext);
ITelegramService telegramService = new TelegramService(new TelegramBotClient("7617124159:AAHzbKa64p9Nlx0c6m0u5M_4m0P1NDtAMbA"));

var handler = new BotHandler(telegramService, driverService,DBorderService, orderService,cityService);



using var cts = new CancellationTokenSource();

var receiverOptions = new ReceiverOptions { AllowedUpdates = null };

telegramService.BotClient.StartReceiving(
    async (botClient, update, cancellationToken) => await handler.HandleUpdateAsync(update),
    async (botClient, exception, cancellationToken) => Console.WriteLine($"Ошибка: {exception.Message}"),
    receiverOptions,
    cts.Token
);

Console.WriteLine("Бот запущен...");
Console.ReadLine();
cts.Cancel();
