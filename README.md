# DeliveryTgBot
# 🚚 DeliveryTgBot

> Telegram-бот для автоматизации приёма и обработки заявок на доставку, написанный на C# / .NET 9 с чистой архитектурой по принципам SOLID.

---

## 📋 О проекте

**DeliveryTgBot** — это Telegram-бот, который позволяет клиентам оформлять заявки на доставку прямо в мессенджере. Бот проводит пользователя через весь процесс: выбор города, ввод адреса (с автодополнением через Яндекс API), указание деталей заказа и подтверждение.

Проект создан как пет-проект для демонстрации навыков построения production-ready приложений на C# с применением SOLID-принципов, Dependency Injection и работы с базой данных через EF Core.

---

## ⚙️ Стек технологий

| Категория | Технология |
|---|---|
| Язык / Платформа | C# 13, .NET 9 |
| Telegram API | Telegram.Bot 22.6 |
| ORM | Entity Framework Core 9 |
| База данных | PostgreSQL (Npgsql) |
| DI / Hosting | Microsoft.Extensions.Hosting |
| HTTP-клиент | Microsoft.Extensions.Http |
| Геокодирование | Yandex Maps Geocoder API |

---

## 🏗️ Архитектура

Проект построен с соблюдением всех пяти принципов SOLID. Подробное описание рефакторинга — в файле [SOLID_REFACTORING.md](./SOLID_REFACTORING.md).

```
DeliveryTgBot/
├── Data/               # DbContext, конфигурация EF Core
├── Handlers/           # Обработчики обновлений Telegram
│   ├── Commands/       # Команды (/start, /reset)
│   └── Callbacks/      # Inline-кнопки (город, водитель)
├── Interfaces/         # Контракты всех сервисов
├── Migrations/         # Миграции EF Core
├── Models/             # Доменные модели (Order, City и др.)
├── Services/           # Бизнес-логика и интеграции
│   └── YandexAddressService.cs  # Интеграция с Яндекс API
├── Helpers/            # Вспомогательные утилиты
├── Program.cs          # Точка входа, регистрация DI
└── appsettings.json    # Конфигурация
```

### Ключевые компоненты

- **`BotHandler`** — оркестратор, принимает обновления от Telegram
- **`MessageProcessor`** — обрабатывает текстовые сообщения и управляет флоу заказа
- **`OrderStateManager`** — управляет состоянием заказа (State Machine)
- **`ICommandHandler` / `ICallbackHandler`** — интерфейсы для расширяемых обработчиков
- **`YandexAddressService`** — автодополнение адреса через Яндекс Геокодер
- **`TelegramKeyboardBuilder`** — построение inline-клавиатур

---

## 🚀 Запуск

### Требования

- .NET 9 SDK
- PostgreSQL
- Telegram Bot Token (получить у [@BotFather](https://t.me/BotFather))
- Yandex API Key (для геокодирования адресов)

### 1. Клонировать репозиторий

```bash
git clone https://github.com/nkikita/DeliveryTgBot.git
cd DeliveryTgBot
```

### 2. Настроить конфигурацию

Отредактировать `appsettings.json`:

```json
{
  "TelegramBotToken": "YOUR_BOT_TOKEN",
  "YandexApiKey": "YOUR_YANDEX_KEY",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=deliverybot;Username=postgres;Password=yourpassword"
  }
}
```

> ⚠️ Никогда не коммитьте реальные токены в репозиторий. Используйте переменные окружения или `.env`-файл.

### 3. Применить миграции

```bash
dotnet ef database update
```

### 4. Запустить

```bash
dotnet run
```

---

## 💬 Функциональность бота

| Команда / Действие | Описание |
|---|---|
| `/start` | Начало оформления заявки |
| `/reset` | Сброс текущего заказа |
| Выбор города | Inline-клавиатура с городами |
| Ввод адреса | Автодополнение через Яндекс API |
| Подтверждение заказа | Сохранение в PostgreSQL |
| Уведомление | Отправка данных оператору |

---

## 🔌 Расширение функциональности

### Добавить новую команду

```csharp
public class MyCommandHandler : ICommandHandler
{
    public bool CanHandle(string command) => command == "/mycommand";
    public async Task HandleAsync(ITelegramBotClient bot, Message message) { ... }
}
```

Зарегистрировать в `Program.cs`:
```csharp
services.AddScoped<ICommandHandler, MyCommandHandler>();
```

### Добавить новый тип callback

Аналогично — реализовать `ICallbackHandler` и зарегистрировать в DI.

---

## 📌 TODO / Планы развития

- [ ] Добавить unit-тесты (xUnit + Moq)
- [ ] Настроить Docker + docker-compose
- [ ] Добавить логирование через Serilog
- [ ] Реализовать административную панель для операторов
- [ ] Добавить CI/CD через GitHub Actions

---

## 👨‍💻 Автор

**Никита Королёв** — C#/.NET разработчик

- GitHub: [@nkikita](https://github.com/nkikita)
- Telegram: [@John10Nikolas](https://t.me/John10Nikolas)
