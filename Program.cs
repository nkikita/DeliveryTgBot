using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Polling;
using DeliveryTgBot.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DeliveryTgBot.Interfaces;
using DeliveryTgBot.Services;
using DeliveryTgBot.Handlers;
using DeliveryTgBot.Handlers.Commands;
using DeliveryTgBot.Handlers.Callbacks;

// Create host builder for dependency injection
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Register configuration service
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        
        // Configure DbContext
        services.AddDbContext<DeliveryDbContext>((serviceProvider, options) =>
        {
            var config = serviceProvider.GetRequiredService<IConfigurationService>();
            options.UseNpgsql(config.DatabaseConnectionString)
                   .LogTo(Console.WriteLine, LogLevel.Information)
                   .EnableSensitiveDataLogging();
        });

        // Register services
        services.AddHttpClient();
        services.AddScoped<IDriverService, DriverService>();
        services.AddScoped<IOrderCacheService, OrderCacheService>();
        services.AddScoped<ICityService, CityService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<ICalendarService, CalendarService>();
        services.AddScoped<IKeyboardBuilder, TelegramKeyboardBuilder>();
        services.AddScoped<IAddressService, YandexAddressService>();
        services.AddScoped<TelegramServiceFactory>();
        services.AddScoped<ITelegramService>(serviceProvider => 
        {
            var factory = serviceProvider.GetRequiredService<TelegramServiceFactory>();
            return factory.Create();
        });
        services.AddScoped<IOrderNotificationService, TelegramOrderNotificationService>();
        
        // Register new SOLID-compliant services
        services.AddScoped<IOrderStateManager, OrderStateManager>();
        services.AddScoped<MessageProcessor>();
        
        // Register command handlers
        services.AddScoped<ICommandHandler, StartCommandHandler>();
        services.AddScoped<ICommandHandler, ResetCommandHandler>();
        
        // Register callback handlers
        services.AddScoped<ICallbackHandler, CityCallbackHandler>();
        services.AddScoped<ICallbackHandler, DriverCallbackHandler>();
        
        // Register main handler
        services.AddScoped<BotHandler>();
    })
    .Build();

// Ensure database is migrated
using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();
    dbContext.Database.Migrate();
}

// Get services from DI container
var telegramService = host.Services.GetRequiredService<ITelegramService>();
var botHandler = host.Services.GetRequiredService<BotHandler>();

// Configure bot
using var cts = new CancellationTokenSource();
var receiverOptions = new ReceiverOptions { AllowedUpdates = null };

telegramService.BotClient.StartReceiving(
    async (botClient, update, cancellationToken) => await botHandler.HandleUpdateAsync(update),
    async (botClient, exception, cancellationToken) => Console.WriteLine($"Ошибка: {exception.Message}"),
    receiverOptions,
    cts.Token
);

Console.WriteLine("Бот запущен...");
Console.ReadLine();
cts.Cancel();
