using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

public class Promotion
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }
    public DateTime ExpirationDate { get; set; }
    public string CouponCode { get; set; }

    public override string ToString()
    {
        return $"🌟 *{Title}*\n{Description}\n`Діє до: {ExpirationDate:dd.MM.yyyy}`";
    }

}

public class Program
{
    private static List<Promotion> promotions = new List<Promotion>
    {
        new Promotion { Title = "Спеціальна знижка", Description = "Отримайте знижку 20% на всі товари до кінця тижня", Category = "Знижки", ExpirationDate = DateTime.Today.AddDays(7), CouponCode = "SPECIAL20" },
        new Promotion { Title = "Безкоштовна доставка", Description = "При замовленні від 500 гривень  безкоштовна доставка", Category = "Доставка", ExpirationDate = DateTime.Today.AddDays(14), CouponCode = "FREEDELIVERY"},
        new Promotion { Title = "Літній розпродаж", Description = "Великі знижки на всю літню колекцію", Category = "Знижки", ExpirationDate = DateTime.Today.AddDays(30), CouponCode = "SUMMER50" },
        new Promotion { Title = "Подарунок за покупку", Description = "Отримайте подарунок за кожну покупку вартістю від 1000 гривень", Category = "Подарунки", ExpirationDate = DateTime.Today.AddDays(10), CouponCode = "GIFT1000" },
        new Promotion { Title = "Ексклюзивна пропозиція для клієнтів", Description = "Тільки для наших постійних клієнтів: додаткова знижка 15% на перший замовлення місяця", Category = "Знижки", ExpirationDate = DateTime.Today.AddDays(20), CouponCode = "LOYALTY15" },
        new Promotion { Title = "Знижка на техніку", Description = "Спеціальна знижка 10% на всю техніку в нашому магазині", Category = "Електроніка", ExpirationDate = DateTime.Today.AddDays(15), CouponCode = "TECHSALE10" }
    };


    public static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        var botClient = new TelegramBotClient("7152185980:AAH6kObr1pdfc6DGxKPPnBaEqU05ooTvsDs");

        using CancellationTokenSource cts = new();

        ReceiverOptions receiverOptions = new()
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        botClient.StartReceiving(
            updateHandler: async (botClient, update, cancellationToken) => await HandleUpdateAsync(botClient, update, cancellationToken),
            pollingErrorHandler: async (botClient, exception, cancellationToken) => await HandlePollingErrorAsync(botClient, exception, cancellationToken),
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );

        var me = await botClient.GetMeAsync();

        Console.WriteLine($"Start listening for @{me.Username}");
        Console.ReadLine();

        cts.Cancel();
    }

    private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { } message)
            return;

        if (message.Text is not { } messageText)
            return;

        var chatId = message.Chat.Id;

        Console.WriteLine($"Received a '{messageText}' message in chat {chatId} from {message?.Chat.FirstName}.");

        var command = messageText.Split(' ').First().ToLower();

        if (command == "/start")
        {
            await SendMainMenuAsync(botClient, chatId, cancellationToken);
        }

        else if (command == "/promotion" || messageText == "Перегляд акцій")
        {
            await SendAllPromotionsAsync(botClient, chatId, cancellationToken);
        }

        else if (command == "/category" || messageText == "Пошук акцій за категоріями")
        {
            await SendCategoriesAsync(botClient, chatId, cancellationToken);
        }

        // Пошук акцій за категорією
        else if (promotions.Any(p => p.Category.ToLower() == messageText.ToLower().Trim()))
        {
            var category = messageText.ToLower().Trim();
            Console.WriteLine($"Пошук акцій в категорії: '{category}'");
            await SendPromotionsByCategoryAsync(botClient, chatId, category, cancellationToken);
            Console.WriteLine("Рекламні пропозиції надіслано.");
        }

        else if (command == "/coupon" || messageText == "Отримати промокод")
        {
            await SendPromotionsWithButtonsAsync(botClient, chatId, cancellationToken);
        }

        else if (promotions.Any(p => messageText.Contains(p.Title)))
        {
            var promotion = promotions.FirstOrDefault(p => messageText.Contains(p.Title));
            if (promotion != null)
            {
                await SendCouponAsync(botClient, chatId, promotion, cancellationToken);
            }
            else
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Акцію не знайдено",
                    cancellationToken: cancellationToken);
            }
        }
        else if (messageText == "Повернутися до головного меню")
        {
            await SendMainMenuAsync(botClient, chatId, cancellationToken);
        }
    }

    private static async Task SendPromotionsWithButtonsAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        var availablePromotionsMessage = "Виберіть акцію, на яку хочете отримати промокод:\n\n";

       // Створення списку кнопок вибору акцій
       var keyboardButtons = promotions.Select((p) => new[]
        {
        new KeyboardButton(p.Title)
    }).ToList();

        var backButton = new KeyboardButton("Повернутися до головного меню");
        keyboardButtons.Add(new[] { backButton });

        // Створюємо клавіатуру з кнопками
        var keyboard = new ReplyKeyboardMarkup(keyboardButtons.ToArray())
        {
            ResizeKeyboard = true
        };

        // Надсилаємо повідомлення з кнопками вибору акції
        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: availablePromotionsMessage,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);
    }


    private static async Task SendCategoriesAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        var categories = promotions.Select(p => p.Category).Distinct().ToList();
        var keyboardButtons = categories.Select(c => new KeyboardButton(c)).ToArray();
        var keyboard = new ReplyKeyboardMarkup(keyboardButtons);

        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Оберіть категорію:",
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);
    }

    private static async Task SendMainMenuAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[]
            {
                new KeyboardButton("Перегляд акцій"),
                new KeyboardButton("Пошук акцій за категоріями"),
                new KeyboardButton("Отримати промокод")
            }
        });

        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Меню:",
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);
    }

    private static async Task SendPromotionsByCategoryAsync(ITelegramBotClient botClient, long chatId, string category, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Searching for promotions in category: '{category}'");

        var filteredPromotions = promotions.Where(p => p.Category.ToLower() == category.ToLower()).ToList();
        if (filteredPromotions.Any())
        {
            var messageText = $"Акції у категорії '{category}':\n\n";
            foreach (var promotion in filteredPromotions)
            {
                messageText += promotion.ToString() + "\n\n";
            }
            Console.WriteLine("Sending promotions");
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: messageText,
                cancellationToken: cancellationToken,
                parseMode: ParseMode.MarkdownV2);
            Console.WriteLine("Promotions sent");

            // Після відправлення акцій за категорією, викликаємо метод SendMainMenuAsync для повернення до головного меню
            await SendMainMenuAsync(botClient, chatId, cancellationToken);
        }
        else
        {
            Console.WriteLine($"No promotions found in category: '{category}'");
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"Акції у категорії '{category}' не знайдено",
                cancellationToken: cancellationToken,
                parseMode: ParseMode.MarkdownV2);

            // Після відправлення повідомлення про відсутність акцій, також викликаємо метод SendMainMenuAsync для повернення до головного меню
            await SendMainMenuAsync(botClient, chatId, cancellationToken);
        }
    }

    private static async Task SendCouponAsync(ITelegramBotClient botClient, long chatId, Promotion promotion, CancellationToken cancellationToken)
    {
        var messageText = $"Ваш промокод для акції '{promotion.Title}': `{promotion.CouponCode}`\nДля отримання знижки перейдіть на наш сайт та використовуйте цей промокод при оформленні замовлення";
        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: messageText,
            cancellationToken: cancellationToken,
            parseMode: ParseMode.MarkdownV2);
    }

    private static async Task SendAllPromotionsAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        var messageText = "Ось усі доступні акції:\n\n";
        foreach (var promotion in promotions)
        {
            messageText += promotion.ToString() + "\n\n";
        }
        Console.WriteLine(messageText);

        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: messageText,
            cancellationToken: cancellationToken,
            parseMode: ParseMode.MarkdownV2);

        Console.WriteLine("Усі рекламні повідомлення надіслано");
    }

    private static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(ErrorMessage);
        return Task.CompletedTask;
    }
}
