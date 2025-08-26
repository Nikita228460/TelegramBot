using Newtonsoft.Json.Linq;
using SkiaSharp;
using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBot
{
    internal class Program
    {
        private static ConcurrentDictionary<long, bool> waitingForUid = new();
        private static ConcurrentDictionary<long, string> userUids = new();
        private static ConcurrentDictionary<string, long> usernameToChatId = new();
        private static Dictionary<long, DateTime> activeSignals = new();
        private const long AdminChatId = 7707787345;

        // Относительные пути
        private const string SourceDirectory = "Source";
        private const string ReferalsFilePath = "Source/Referals.txt";
        private const string BotDescriptionImagePath = "Source/Copilot_20250819_192744.png";
        private const string UID1ImagePath = "Source/1.png";
        private const string UID2ImagePath = "Source/2.png";

        // Текстовые константы
        private const string StartTradeGuide =
            "📋 *Чтобы получить доступ к функциям бота, выполните шаги:*\n\n" +

            "*1. Регистрация на MEXC (без KYC):*\n" +
            "   🔗 Быстрая регистрация (код уже введён):\n" +
            "      https://promote.mexc.com/r/8pvI5A3t\n" +
            "   🔗 Стандартная регистрация:\n" +
            "      mexc.com → код при регистрации: `3Lkno`\n\n" +

            "⚠️ Без кода доступ к боту *не предоставляется*!\n" +
            "Если вы уже зарегистрированы без кода → пишите: @azizmirzoev\\_26\n\n" +

            "*2. После регистрации:*\n" +
            "   *-* Перейдите в раздел \"Привязать UID\"\n" +
            "   *-* Отправьте ваш UID (инструкция внутри)\n" +
            "   *-* После одобрения вы получите доступ ко всем функциям бота\n\n" +

            "📚 *Новичок в трейдинге?* → Изучите раздел \"📖 Полный гайд\"\n\n" +

            "*Примечание:* Код действует только для новых пользователей.";

        private const string BotDescription =
            "🤖 *Я — крипто-ассистент на основе искусственного интеллекта*\n\n" +
            "*Версия AI:* NeurolinkPredictor v3.2 (усовершенствованная архитектура на базе GPT-4, обученная на реальных рыночных данных и паттернах алгоритмической торговли)\n\n" +

            "*Что я делаю:*\n\n" +

            "📊 *Глубокий анализ в реальном времени*\n" +
            "Моя модель анализирует BTC/USDT с учётом:\n" +
            "- ценовых паттернов и технических индикаторов\n" +
            "- объёмов торгов и динамики ордербуков\n" +
            "- волатильности и рыночных настроений\n\n" +

            "🎯 *Торговые сигналы*\n" +
            "Я формирую готовые идеи с полным набором параметров:\n" +
            "- Пара и направление сделки (LONG/SHORT)\n" +
            "- Цена входа и сила тренда\n" +
            "- Уровни Take-Profit и Stop-Loss\n" +
            "- Расчёт Risk/Reward\n" +
            "- Волатильность, вероятность и длительность сигнала\n" +
            "- Время начала и завершения сделки";

        private const string UIDDescription = "Отправьте ваш UID ⬇️\n(Ваша заявка будет обработана в течении 10 минут)";
        private const string WaitingForBinding = "⏳ Ваша заявка обрабатывается...";

        // Гайды
        private const string GuideTrading =
            "*ТОРГОВЛЯ ФЬЮЧЕРСАМИ (USDT-M) НА MEXC*\n\n" +

            "*Подготовка счёта:*\n" +
            "1) Переведите USDT на фьючерсный счёт: Кошелёк → Перевод → Из Спот в Фьючерсы (USDT-M) → Выберите USDT → Введите сумму → Подтвердите.\n" +
            "2) Перейдите в раздел фьючерсов USDT-M.\n\n" +

            "*Рекомендуемые настройки перед началом:*\n" +
            "• Режим маржи: *Изолированная маржа (Isolated)* — риск ограничен рамками позиции.\n" +
            "• Кредитное плечо: выбирайте умеренное.\n" +
            "• Включите поля TP/SL (укажите ROE) — чтобы задать цели и защиту сразу при входе.\n\n" +

            "*Как открыть позицию (Market-ордер):*\n" +
            "1) Выберите пару: BTC/USDT.\n" +
            "2) Убедитесь в режимах: Изолированная маржа, выбранное плечо.\n" +
            "3) Выберите тип ордера: *Market* (по рынку).\n" +
            "4) Укажите объём входа в USDT (в соответствии с управлением риском и сигналом).\n" +
            "5) Задайте *Stop-Loss* и *Take-Profit* в процентах согласно сигналу.\n" +
            "6) Нажмите *Buy/Long* (если ожидается рост) или *Sell/Short* (если ожидается падение).\n" +
            "7) Проверьте данные во всплывающем окне и подтвердите сделку.\n\n" +

            "*Как открыть позицию (Limit-ордер):*\n" +
            "1) Тип ордера: *Limit*.\n" +
            "2) Укажите желаемую цену входа и объём.\n" +
            "3) Задайте *TP/SL* заранее или сразу после исполнения ордера.\n" +
            "4) Разместите ордер. До исполнения его можно изменить или отменить в списке «Ордера».\n\n" +

            "*Где смотреть позицию:*\n" +
            "• Раздел «Позиции/Positions»: там видны размер, сторона (Long/Short), средняя цена входа, нереализованный PnL, ROE, ликвидационная цена и установленные TP/SL.\n\n" +

            "*Как закрыть позицию:*\n" +
            "• Автоматически — по заранее выставленным TP/SL.\n" +
            "• Вручную — кнопка «Закрыть/Close» → тип *Market* для мгновенного выхода.\n" +
            "• Частично — укажите долю (например, 25% - 50% - 75%) или количество и подтвердите закрытие.\n\n" +

            "*Памятка по сигналам бота:*\n" +
            "• Вход: либо по рынку (Market), либо по цене в сигнале.\n" +
            "• Уровни: выставляйте TP/SL строго в соответствии с сигналом (в процентах).\n" +
            "• Длительность: сигнал активен ограниченное время; по его завершении запросите новый.\n";

        private const string GuideRiskManagement =
             "*РИСКИ* \n\n" +

             "Успешная торговля строится на балансе: качество сигналов + дисциплина трейдера. " +
             "Наш бот показывает до *80% успешных сделок*, но результат напрямую зависит от того, " +
             "как вы управляете своим депозитом.\n\n" +

             "*Основные правила управления рисками:*\n\n" +

             "• *Размер позиции* — оптимально использовать 3–15% от депозита в сделке. " +
             "Никогда не рискуйте всем капиталом.\n\n" +

             "• *Стоп-лосс* — устанавливайте всегда при входе. Это ключевая защита капитала " +
             "и основа долгосрочной работы.\n\n" +

             "• *Кредитное плечо* — не используйте слишком большие плечи. " +
             "Чем выше плечо, тем ближе цена ликвидации и тем выше риск потери депозита.";

        private const string GuideSupport =
            "*ТЕХНИЧЕСКАЯ ПОДДЕРЖКА* \n\n" +
            "*Если возникли проблемы:*\n\n" +
            "*• По пополнению:* support@mexc.com\n" +
            "*• По торговле:* help-center на сайте\n" +
            "*• По работе бота:* @azizmirzoev\\_26\n\n";

        // Стратегии
        private const string StrategySmallDeposits =
            "*СТРАТЕГИЯ ДЛЯ МАЛЕНЬКИХ ДЕПОЗИТОВ ($10–100)*\n\n" +
            "1. Рекомендуемое кредитное плечо: 10–15x.\n" +
            "2. Размер позиции: 10–15% от депозита на одну сделку.\n" +
            "3. Основная цель: постепенное и безопасное наращивание депозита.\n\n" +
            "Практические рекомендации:\n" +
            "- Начинайте с минимальных сделок ($1–2), чтобы контролировать риски.\n" +
            "- Прибыль реинвестируйте постепенно, избегая резких увеличений объема сделки.\n" +
            "- Избегайте соблазна увеличивать плечо после успешных сделок.\n";

        // Для средних депозитов
        private const string StrategyMediumDeposits =
            "*СТРАТЕГИЯ ДЛЯ СРЕДНИХ ДЕПОЗИТОВ ($100–1000)*\n\n" +
            "1. Рекомендуемое кредитное плечо: 8–12x.\n" +
            "2. Размер позиции: 3–5% от депозита на одну сделку.\n" +
            "3. Цель: 5–10% стабильной прибыли в неделю.\n\n" +
            "Практические рекомендации:\n" +
            "- Ведите торговый журнал: фиксируйте вход, выход, результат и эмоции. Это поможет выявить ошибки.\n" +
            "- Используйте лимитные ордера для более точного входа и выхода.\n" +
            "- Раз в неделю фиксируйте часть прибыли и не допускайте полного реинвестирования.\n" +
            "- Контролируйте эмоции: жадность и азарт — основные враги стабильного трейдера.\n";

        // Для больших депозитов
        private const string StrategyLargeDeposits =
            "*СТРАТЕГИЯ ДЛЯ БОЛЬШИХ ДЕПОЗИТОВ ($1000+)*\n\n" +
            "1. Рекомендуемое кредитное плечо: 5–8x.\n" +
            "2. Размер позиции: 1–3% от депозита на одну сделку.\n" +
            "3. Цель: 3–5% стабильной прибыли в неделю при минимизации рисков.\n\n" +
            "Практические рекомендации:\n" +
            "- Делите депозит на несколько частей, не задействуйте более 30% капитала одновременно.\n" +
            "- Работайте на качество сделок, а не на количество. Лучше меньше сделок, но с высокой вероятностью успеха.\n" +
            "- Инвестируйте только свободные средства, не затрагивая личный бюджет.\n";

        // Общие правила
        private const string StrategyGeneralRules =
            "*ОБЩИЕ ПРАВИЛА ДЛЯ ВСЕХ ТРЕЙДЕРОВ*\n\n" +
            "Золотые принципы:\n" +
            "1. Дисциплина важнее эмоций. Следуйте стратегии и сигналам.\n" +
            "2. Стоп-лосс обязателен в каждой сделке. Его отсутствие ведет к потере депозита.\n" +
            "3. Фиксируйте прибыль регулярно. Небольшая прибыль лучше крупного убытка.\n" +
            "4. Анализируйте каждую сделку. Ошибки — это опыт, если вы делаете выводы.\n\n" +
            "Как читать сигналы:\n" +
            "- LONG = открываем покупку, ожидаем рост.\n" +
            "- SHORT = открываем продажу, ожидаем падение.\n" +
            "- Время = срок актуальности сигнала.\n" +
            "- Уровни = тейк-профит и стоп-лосс.\n" +
            "- Вероятность = расчетная вероятность успеха.\n\n" +
            "Советы по работе на MEXC:\n" +
            "- Используйте изолированную маржу.\n" +
            "- Включайте Auto-Margin для снижения риска ликвидации.\n" +
            "- Начинайте с малых депозитов при отсутствии опыта.\n\n" +
            "Что нельзя делать:\n" +
            "- Не используйте плечо 50–100x.\n" +
            "- Не рискуйте более чем 15% депозита за сделку.\n" +
            "- Не торгуйте на весь депозит.\n" +
            "- Не убирайте стоп-лосс.\n" +
            "- Не поддавайтесь эмоциям и чужим советам без анализа.\n";

        static async Task Main(string[] args)
        {
            // Создаем директорию если не существует
            if (!Directory.Exists(SourceDirectory))
                Directory.CreateDirectory(SourceDirectory);

            string apiKey = "8430329688:AAGs72sqkFFC6xtmmqGijr7D7as3bF28iK4";
            var botClient = new TelegramBotClient(apiKey);

            using var cts = new CancellationTokenSource();

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }
            };

            botClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cts.Token
            );

            var me = await botClient.GetMe();
            Console.WriteLine($"Бот @{me.Username} запущен.");

            // Блокируем главный поток навсегда, чтобы бот продолжал работать
            await Task.Delay(-1);

            // cts.Cancel(); // не нужен, так как процесс держим навсегда
        }

        private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                if (update.Type == UpdateType.CallbackQuery)
                {
                    await HandleCallbackQueryAsync(botClient, update.CallbackQuery, cancellationToken);
                    return;
                }

                if (update.Message is not { } message)
                    return;

                if (message.Text is not { } messageText)
                    return;

                Console.WriteLine($"Сообщение от {message.Chat.Id}: {messageText}");

                long chatId = message.Chat.Id;
                string username = message.From?.Username ?? message.From?.FirstName ?? "UnknownUser";

                usernameToChatId[username] = chatId;

                if (messageText == "/start")
                {
                    waitingForUid.TryAdd(chatId, false);

                    var keyboard = new ReplyKeyboardMarkup(new[]
                    {
                        new KeyboardButton[] { "📈 Начать", "📊 Что умеет бот?" },
                        new KeyboardButton[] { "Привязать UID", "📖 Полный гайд" },
                        new KeyboardButton[] { "🛎️ Получить сигнал" }
                    })
                    {
                        ResizeKeyboard = true
                    };

                    await botClient.SendMessage(
                        chatId: chatId,
                        text: $"👋 *Добро пожаловать, {message.From.FirstName}!*\n\n" +
                              "🚀 *Я — ваш персональный трейдинг-ассистент*\n\n" +
                              "• 📊 Анализирую рынок в реальном времени\n" +
                              "• 🎯 Генерирую торговые сигналы\n" +
                              "• ⚡ Оптимизирован под биржу MEXC\n" +
                              "• 🤖 Работаю 24/7\n\n" +
                              "✨ Чтобы получить доступ к сигналам — нажмите кнопку *Начать*.\n\n" +
                              "📢 Наш Telegram-канал: [Подписаться](https://t.me/CryptoAI_MEXC)",
                        parseMode: ParseMode.Markdown,
                        replyMarkup: keyboard,
                        cancellationToken: cancellationToken
                    );
                }
                else if (messageText == "📈 Начать")
                {
                    waitingForUid[chatId] = false;
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: StartTradeGuide,
                        parseMode: ParseMode.Markdown,
                        cancellationToken: cancellationToken
                    );
                }
                else if (messageText == "📖 Полный гайд")
                {
                    waitingForUid[chatId] = false;

                    var sectionKeyboard = new ReplyKeyboardMarkup(new[]
                    {
                        new KeyboardButton[] { "Торговля", "Стратегии" },
                        new KeyboardButton[] { "Риски", "Поддержка" },
                        new KeyboardButton[] { "<- Назад в меню" }
                    })
                    {
                        ResizeKeyboard = true
                    };

                    await botClient.SendMessage(
                        chatId: chatId,
                        text: "📖 *Выберите раздел гайда:*\n\n" +
                              "1. *Торговля* - начало работы с фьючерсами\n" +
                              "2. *Стратегии* - правильная работа с ботом\n" +
                              "3. *Риски* - управление рисками\n" +
                              "4. *Поддержка* - техническая помощь\n" +
                              "👇 Выберите нужный раздел:",
                        parseMode: ParseMode.Markdown,
                        replyMarkup: sectionKeyboard,
                        cancellationToken: cancellationToken
                    );
                }
                else if (messageText == "Торговля")
                {
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: GuideTrading,
                        parseMode: ParseMode.Markdown,
                        cancellationToken: cancellationToken
                    );
                }
                else if (messageText == "Риски")
                {
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: GuideRiskManagement,
                        parseMode: ParseMode.Markdown,
                        cancellationToken: cancellationToken
                    );
                }
                else if (messageText == "Стратегии")
                {
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: StrategySmallDeposits,
                        parseMode: ParseMode.Markdown,
                        cancellationToken: cancellationToken
                    );

                    await botClient.SendMessage(
                        chatId: chatId,
                        text: StrategyMediumDeposits,
                        parseMode: ParseMode.Markdown,
                        cancellationToken: cancellationToken
                    );

                    await botClient.SendMessage(
                        chatId: chatId,
                        text: StrategyLargeDeposits,
                        parseMode: ParseMode.Markdown,
                        cancellationToken: cancellationToken
                    );

                    await botClient.SendMessage(
                        chatId: chatId,
                        text: StrategyGeneralRules,
                        parseMode: ParseMode.Markdown,
                        cancellationToken: cancellationToken
                    );
                }
                else if (messageText == "Поддержка")
                {
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: GuideSupport,
                        parseMode: ParseMode.Markdown,
                        cancellationToken: cancellationToken
                    );
                }
                else if (messageText == "<- Назад в меню")
                {
                    var mainKeyboard = new ReplyKeyboardMarkup(new[]
                    {
                        new KeyboardButton[] { "📈 Начать", "📊 Что умеет бот?" },
                        new KeyboardButton[] { "Привязать UID", "📖 Полный гайд" },
                        new KeyboardButton[] { "🛎️ Получить сигнал" }
                    })
                    {
                        ResizeKeyboard = true
                    };

                    await botClient.SendMessage(
                        chatId: chatId,
                        text: "Главное меню.",
                        replyMarkup: mainKeyboard,
                        cancellationToken: cancellationToken
                    );
                }
                else if (messageText == "Привязать UID")
                {
                    bool alreadyExists = false;
                    if (File.Exists(ReferalsFilePath))
                    {
                        var lines = await File.ReadAllLinesAsync(ReferalsFilePath);
                        alreadyExists = lines.Any(line => line.StartsWith(username + " "));
                    }

                    if (alreadyExists)
                    {
                        await botClient.SendMessage(
                            chatId: chatId,
                            text: "⚠️ Вы уже отправили UID!",
                            cancellationToken: cancellationToken
                        );
                        return;
                    }

                    waitingForUid[chatId] = true;

                    // Отправляем инструкции с картинками
                    if (File.Exists(UID1ImagePath))
                    {
                        await using var stream1 = File.OpenRead(UID1ImagePath);
                        await botClient.SendPhoto(
                            chatId: chatId,
                            photo: InputFile.FromStream(stream1, "uid_instruction_1.png"),
                            caption: "📋 *Шаг 1:* Найдите ваш UID в личном кабинете MEXC",
                            parseMode: ParseMode.Markdown,
                            cancellationToken: cancellationToken
                        );
                    }

                    if (File.Exists(UID2ImagePath))
                    {
                        await using var stream2 = File.OpenRead(UID2ImagePath);
                        await botClient.SendPhoto(
                            chatId: chatId,
                            photo: InputFile.FromStream(stream2, "uid_instruction_2.png"),
                            caption: "📋 *Шаг 2:* Скопируйте UID и отправьте боту",
                            parseMode: ParseMode.Markdown,
                            cancellationToken: cancellationToken
                        );
                    }

                    await botClient.SendMessage(
                        chatId: chatId,
                        text: UIDDescription,
                        parseMode: ParseMode.Markdown,
                        cancellationToken: cancellationToken
                    );
                }
                else if (messageText == "📊 Что умеет бот?")
                {
                    waitingForUid[chatId] = false;

                    if (File.Exists(BotDescriptionImagePath))
                    {
                        await using var stream = File.OpenRead(BotDescriptionImagePath);
                        await botClient.SendPhoto(
                            chatId: chatId,
                            photo: InputFile.FromStream(stream, "bot_description.png"),
                            caption: BotDescription,
                            parseMode: ParseMode.Markdown,
                            cancellationToken: cancellationToken
                        );
                    }
                    else
                    {
                        await botClient.SendMessage(
                            chatId: chatId,
                            text: BotDescription,
                            parseMode: ParseMode.Markdown,
                            cancellationToken: cancellationToken
                        );
                    }
                }
                else if (messageText == "🛎️ Получить сигнал")
                {
                    waitingForUid[chatId] = false;

                    bool hasAccess = false;
                    if (File.Exists(ReferalsFilePath))
                    {
                        var lines = await File.ReadAllLinesAsync(ReferalsFilePath);
                        foreach (var raw in lines.Reverse())
                        {
                            var line = raw.Trim();
                            if (string.IsNullOrWhiteSpace(line)) continue;

                            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length < 3) continue;

                            var nameFromFile = parts[0];
                            if (string.Equals(nameFromFile, username, StringComparison.OrdinalIgnoreCase))
                            {
                                if (bool.TryParse(parts[2], out bool accessFromFile))
                                    hasAccess = accessFromFile;
                                break;
                            }
                        }
                    }

                    if (!hasAccess)
                    {
                        await botClient.SendMessage(
                            chatId: chatId,
                            text: "⚠️ У вас нет доступа к этой функции!",
                            cancellationToken: cancellationToken
                        );
                        return;
                    }

                    if (activeSignals.ContainsKey(chatId))
                    {
                        await botClient.SendMessage(
                            chatId: chatId,
                            text: "⚠️ У вас уже есть активный сигнал! Дождитесь его завершения.",
                            cancellationToken: cancellationToken
                        );
                        return;
                    }

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            Random rnd = new Random();
                            int waitTime = rnd.Next(10, 41);

                            var waitingMessage = await botClient.SendMessage(
                                chatId: chatId,
                                text: "🤖 AI запускает анализ рынка...",
                                cancellationToken: cancellationToken
                            );

                            decimal btcPrice;
                            decimal volatility;
                            string direction;
                            decimal trendStrength;
                            string chartFilePath;

                            using (HttpClient http = new HttpClient())
                            {
                                http.Timeout = TimeSpan.FromSeconds(30);

                                await UpdateProgress("📊 Получение данных...", 20);

                                string priceUrl = "https://api.binance.com/api/v3/ticker/price?symbol=BTCUSDT";
                                string priceJson = await http.GetStringAsync(priceUrl);
                                var priceData = JObject.Parse(priceJson);
                                btcPrice = decimal.Parse((string)priceData["price"], CultureInfo.InvariantCulture);

                                await UpdateProgress("📈 Анализ трендов...", 40);

                                var analysisResults = new List<MarketAnalysis>();
                                var timeframes = new[] { ("1h", 0.3m), ("4h", 0.5m), ("1d", 0.8m) };

                                foreach (var (timeframe, weight) in timeframes)
                                {
                                    string klineUrl = $"https://api.binance.com/api/v3/klines?symbol=BTCUSDT&interval={timeframe}&limit=50";
                                    string klineJson = await http.GetStringAsync(klineUrl);
                                    var klines = JArray.Parse(klineJson);

                                    var analysis = AnalyzeTimeframe(klines, timeframe, weight);
                                    analysisResults.Add(analysis);
                                }

                                await UpdateProgress("📉 Расчет волатильности...", 60);

                                trendStrength = analysisResults.Average(a => a.TrendStrength * a.Weight) /
                                              analysisResults.Sum(a => a.Weight);

                                volatility = analysisResults.Average(a => a.Volatility);

                                if (trendStrength > 0.15m)
                                {
                                    direction = "LONG";
                                }
                                else if (trendStrength < -0.15m)
                                {
                                    direction = "SHORT";
                                }
                                else
                                {
                                    var recentAnalysis = analysisResults.First(a => a.Timeframe == "1h");
                                    direction = recentAnalysis.LastDirection;
                                    if (rnd.Next(100) < 35)
                                        direction = direction == "LONG" ? "SHORT" : "LONG";
                                }

                                await UpdateProgress("🖼️ Генерация графика...", 80);
                                chartFilePath = await GenerateChart(http);
                            }

                            await UpdateProgress("✅ Анализ завершен!", 100);
                            await Task.Delay(1000);

                            var (stopLoss, takeProfit, probability) = GenerateTradingParameters(volatility, trendStrength, direction);

                            int durationMinutes = volatility switch
                            {
                                < 0.8m => 45 + rnd.Next(0, 31),
                                < 1.5m => 30 + rnd.Next(0, 31),
                                _ => 20 + rnd.Next(0, 21)
                            };

                            DateTime startTime = DateTime.Now;
                            DateTime endTime = startTime.AddMinutes(durationMinutes);

                            string trendEmoji = trendStrength switch
                            {
                                > 0.3m => "🚀",
                                > 0.15m => "📈",
                                > -0.15m => "↔️",
                                > -0.3m => "📉",
                                _ => "🔻"
                            };

                            string signalText = $"""
                                🤖 *AI Trading Signal [MEXC]*
                                
                                🎯 *Основные параметры*
                                🔹 Пара: *BTC/USDT*
                                🔹 Направление: *{direction}* {trendEmoji}
                                🔹 Сила тренда: *{trendStrength:+#0.0;-#0.0}*
                                🔹 Цена входа: *${btcPrice:##,##0}*

                                ⚡ *Торговые уровни*
                                🔹 Take-Profit: *+{takeProfit}%*
                                🔹 Stop-Loss: *-{stopLoss}%*
                                🔹 Risk/Reward: *1:{takeProfit / stopLoss:0.0}*

                                📊 *Аналитика*
                                🔹 Волатильность: *{volatility:0.0}%*
                                🔹 Вероятность: *{probability}%*
                                🔹 Длительность: *{durationMinutes} мин*

                                ⏰ *Время*
                                🕒 Начало: {startTime:HH:mm}
                                ⏳ Конец: {endTime:HH:mm}
                                📅 Дата: {startTime:yyyy-MM-dd}
                                """;

                            if (File.Exists(chartFilePath))
                            {
                                using var stream = File.OpenRead(chartFilePath);
                                var inputFile = InputFile.FromStream(stream, "btc_analysis.png");

                                await botClient.SendPhoto(
                                    chatId: chatId,
                                    photo: inputFile,
                                    caption: signalText,
                                    parseMode: ParseMode.Markdown,
                                    cancellationToken: cancellationToken
                                );
                            }
                            else
                            {
                                await botClient.SendMessage(
                                    chatId: chatId,
                                    text: signalText,
                                    parseMode: ParseMode.Markdown,
                                    cancellationToken: cancellationToken
                                );
                            }

                            activeSignals[chatId] = endTime;
                            await Task.Delay(endTime - DateTime.Now);
                            activeSignals.Remove(chatId);

                            await botClient.SendMessage(
                                chatId: chatId,
                                text: "✅ Сигнал завершен. Доступен новый анализ.",
                                cancellationToken: cancellationToken
                            );

                            try { File.Delete(chartFilePath); } catch { }

                            async Task UpdateProgress(string text, int percent)
                            {
                                int steps = 10;
                                string progressBar = new string('▓', percent / 10) + new string('▒', (100 - percent) / 10);

                                await botClient.EditMessageText(
                                    chatId: chatId,
                                    messageId: waitingMessage.MessageId,
                                    text: $"{text}\n{progressBar} {percent}%",
                                    cancellationToken: cancellationToken
                                );

                                await Task.Delay(800);
                            }

                            MarketAnalysis AnalyzeTimeframe(JArray klines, string timeframe, decimal weight)
                            {
                                var prices = klines.Select(k =>
                                    decimal.Parse((string)k[4], CultureInfo.InvariantCulture)).ToArray();

                                var highs = klines.Select(k =>
                                    decimal.Parse((string)k[2], CultureInfo.InvariantCulture)).ToArray();

                                var lows = klines.Select(k =>
                                    decimal.Parse((string)k[3], CultureInfo.InvariantCulture)).ToArray();

                                decimal totalRange = 0;
                                for (int i = 0; i < 24 && i < highs.Length; i++)
                                {
                                    decimal rangePercent = (highs[i] - lows[i]) / lows[i] * 100;
                                    totalRange += rangePercent;
                                }
                                decimal vol = totalRange / Math.Min(24, highs.Length);

                                decimal sma20 = prices.TakeLast(20).Average();
                                decimal sma50 = prices.Average();
                                decimal rsi = CalculateRSI(prices);

                                decimal strength = 0;

                                if (prices[^1] > sma50) strength += 0.3m;
                                if (prices[^1] < sma50) strength -= 0.3m;
                                if (sma20 > sma50) strength += 0.2m;
                                if (sma20 < sma50) strength -= 0.2m;

                                if (rsi > 65) strength -= 0.1m;
                                if (rsi < 35) strength += 0.1m;

                                if (prices[^1] > prices[^5]) strength += 0.2m;
                                if (prices[^1] < prices[^5]) strength -= 0.2m;

                                string lastDir = prices[^1] > prices[^2] ? "LONG" : "SHORT";

                                return new MarketAnalysis(timeframe, weight, strength, vol, lastDir);
                            }

                            decimal CalculateRSI(decimal[] prices)
                            {
                                if (prices.Length < 15) return 50;

                                decimal[] gains = new decimal[14];
                                decimal[] losses = new decimal[14];

                                for (int i = 1; i < 15; i++)
                                {
                                    decimal change = prices[^i] - prices[^(i + 1)];
                                    gains[i - 1] = Math.Max(0, change);
                                    losses[i - 1] = Math.Max(0, -change);
                                }

                                decimal avgGain = gains.Average();
                                decimal avgLoss = losses.Average();

                                if (avgLoss == 0) return 100;
                                decimal rs = avgGain / avgLoss;
                                return 100 - (100 / (1 + rs));
                            }

                            async Task<string> GenerateChart(HttpClient http)
                            {
                                try
                                {
                                    string chartUrl = "https://api.binance.com/api/v3/klines?symbol=BTCUSDT&interval=15m&limit=20";
                                    string chartJson = await http.GetStringAsync(chartUrl);
                                    var klines = JArray.Parse(chartJson);

                                    var points = klines.Select(k => new
                                    {
                                        Time = DateTimeOffset.FromUnixTimeMilliseconds((long)k[0]).LocalDateTime,
                                        Open = decimal.Parse((string)k[1], CultureInfo.InvariantCulture),
                                        High = decimal.Parse((string)k[2], CultureInfo.InvariantCulture),
                                        Low = decimal.Parse((string)k[3], CultureInfo.InvariantCulture),
                                        Close = decimal.Parse((string)k[4], CultureInfo.InvariantCulture)
                                    }).ToList();

                                    int width = 800;
                                    int height = 400;
                                    int padding = 40;

                                    using var surface = SKSurface.Create(new SKImageInfo(width, height));
                                    var canvas = surface.Canvas;

                                    // Очищаем фон
                                    canvas.Clear(SKColors.Black);

                                    decimal minPrice = points.Min(p => p.Low);
                                    decimal maxPrice = points.Max(p => p.High);
                                    decimal priceRange = maxPrice - minPrice;

                                    float chartWidth = width - padding * 2;
                                    float chartHeight = height - padding * 2;
                                    float scaleX = chartWidth / (points.Count - 1);
                                    float scaleY = chartHeight / (float)priceRange;

                                    // Рисуем свечи
                                    for (int i = 0; i < points.Count; i++)
                                    {
                                        var p = points[i];
                                        bool isBullish = p.Close > p.Open;

                                        float x = padding + i * scaleX;
                                        float highY = padding + chartHeight - (float)(p.High - minPrice) * scaleY;
                                        float lowY = padding + chartHeight - (float)(p.Low - minPrice) * scaleY;
                                        float openY = padding + chartHeight - (float)(p.Open - minPrice) * scaleY;
                                        float closeY = padding + chartHeight - (float)(p.Close - minPrice) * scaleY;

                                        // Линия high-low
                                        using var linePaint = new SKPaint
                                        {
                                            Color = isBullish ? SKColors.Lime : SKColors.Red,
                                            StrokeWidth = 2,
                                            IsAntialias = true
                                        };
                                        canvas.DrawLine(x, highY, x, lowY, linePaint);

                                        // Тело свечи
                                        float bodyTop = Math.Min(openY, closeY);
                                        float bodyBottom = Math.Max(openY, closeY);
                                        float bodyHeight = Math.Abs(openY - closeY);

                                        if (bodyHeight < 2) bodyHeight = 2;

                                        using var bodyPaint = new SKPaint
                                        {
                                            Color = isBullish ? SKColors.Lime : SKColors.Red,
                                            IsStroke = false,
                                            IsAntialias = true
                                        };
                                        canvas.DrawRect(x - 3, bodyTop, 6, bodyHeight, bodyPaint);
                                    }

                                    // Добавляем заголовок
                                    using var textPaint = new SKPaint
                                    {
                                        Color = SKColors.White,
                                        TextSize = 16,
                                        IsAntialias = true,
                                        TextAlign = SKTextAlign.Center
                                    };
                                    canvas.DrawText("BTC/USDT - 15m Chart", width / 2, 20, textPaint);

                                    // Сохраняем изображение
                                    using var image = surface.Snapshot();
                                    using var data = image.Encode(SKEncodedImageFormat.Png, 100);

                                    string path = Path.Combine(Path.GetTempPath(), $"btc_chart_{DateTime.Now.Ticks}.png");
                                    using (var stream = File.OpenWrite(path))
                                    {
                                        data.SaveTo(stream);
                                    }

                                    return path;
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Ошибка генерации графика: {ex.Message}");
                                    return null;
                                }
                            }

                            (decimal stopLoss, decimal takeProfit, int probability) GenerateTradingParameters(
                                decimal volatility, decimal trendStrength, string direction)
                            {
                                Random rnd = new Random();
                                decimal baseSL = volatility switch
                                {
                                    < 0.8m => 0.8m + (decimal)rnd.NextDouble() * 0.4m,
                                    < 1.5m => 1.2m + (decimal)rnd.NextDouble() * 0.6m,
                                    _ => 1.8m + (decimal)rnd.NextDouble() * 0.8m
                                };

                                decimal trendFactor = 1 - (Math.Abs(trendStrength) * 0.3m);
                                decimal stopLoss = Math.Round(baseSL * trendFactor, 1);

                                decimal baseMultiplier = 2.0m + (Math.Abs(trendStrength) * 1.0m);
                                decimal takeProfit = Math.Round(stopLoss * baseMultiplier, 1);

                                decimal rrRatio = takeProfit / stopLoss;
                                int prob = rrRatio switch
                                {
                                    > 3.0m => 60,
                                    > 2.5m => 65,
                                    > 2.0m => 70,
                                    > 1.5m => 75,
                                    _ => 80
                                };

                                if ((direction == "LONG" && trendStrength > 0) ||
                                    (direction == "SHORT" && trendStrength < 0))
                                {
                                    prob += 5;
                                }

                                prob = Math.Clamp(prob + rnd.Next(-3, 4), 60, 85);

                                return (stopLoss, takeProfit, prob);
                            }
                        }
                        catch (Exception ex)
                        {
                            await botClient.SendMessage(
                                chatId: chatId,
                                text: $"❌ Ошибка анализа: {ex.Message}",
                                cancellationToken: cancellationToken
                            );
                        }
                    }, cancellationToken);
                }
                else
                {
                    if (waitingForUid.TryGetValue(chatId, out bool isWaiting) && isWaiting)
                    {
                        string uid = messageText;
                        userUids[chatId] = uid;
                        waitingForUid[chatId] = false;

                        string line = $"{username} {uid} false";

                        try
                        {
                            await File.AppendAllTextAsync(ReferalsFilePath, line + Environment.NewLine);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ошибка при записи UID в файл: {ex.Message}");
                        }

                        await botClient.SendMessage(
                            chatId: chatId,
                            text: WaitingForBinding,
                            parseMode: ParseMode.Markdown,
                            cancellationToken: cancellationToken
                        );

                        string timeNow = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        string notifyText = $"📩 Новый UID\n👤 Пользователь: @{username}\n🆔 UID: {uid}\n⏰ Время: {timeNow}";

                        var inlineKeyboard = new InlineKeyboardMarkup(new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("✅ Одобрить", $"approve:{username}:{uid}"),
                                InlineKeyboardButton.WithCallbackData("❌ Отклонить", $"reject:{username}:{uid}")
                            }
                        });

                        await botClient.SendMessage(
                            chatId: AdminChatId,
                            text: notifyText,
                            parseMode: ParseMode.None,
                            replyMarkup: inlineKeyboard,
                            cancellationToken: cancellationToken
                        );
                    }
                    else
                    {
                        await botClient.SendMessage(
                            chatId: chatId,
                            text: "Я не понял команду 🤔",
                            cancellationToken: cancellationToken
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в HandleUpdateAsync: {ex.Message}");
            }
        }

        public class MarketAnalysis
        {
            public string Timeframe { get; }
            public decimal Weight { get; }
            public decimal TrendStrength { get; }
            public decimal Volatility { get; }
            public string LastDirection { get; }

            public MarketAnalysis(string timeframe, decimal weight, decimal trendStrength,
                                 decimal volatility, string lastDirection)
            {
                Timeframe = timeframe;
                Weight = weight;
                TrendStrength = trendStrength;
                Volatility = volatility;
                LastDirection = lastDirection;
            }
        }

        private static async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            try
            {
                if (callbackQuery.From.Id != AdminChatId)
                {
                    await botClient.AnswerCallbackQuery(callbackQuery.Id, "❌ У вас нет прав для этого действия", cancellationToken: cancellationToken);
                    return;
                }

                string[] parts = callbackQuery.Data.Split(':');
                string action = parts[0];
                string username = parts[1];
                string uid = parts[2];

                if (!File.Exists(ReferalsFilePath))
                {
                    await botClient.AnswerCallbackQuery(callbackQuery.Id, "❌ Файл с UID не найден", cancellationToken: cancellationToken);
                    return;
                }

                var lines = File.ReadAllLines(ReferalsFilePath).ToList();
                bool found = false;
                string resultMessage = "";

                if (action == "approve")
                {
                    for (int i = 0; i < lines.Count; i++)
                    {
                        if (lines[i].StartsWith($"{username} {uid} false"))
                        {
                            lines[i] = $"{username} {uid} true";
                            found = true;
                            resultMessage = $"✅ UID {uid} пользователя {username} одобрен!";
                            break;
                        }
                    }
                }
                else if (action == "reject")
                {
                    for (int i = 0; i < lines.Count; i++)
                    {
                        if (lines[i].StartsWith($"{username} {uid}"))
                        {
                            lines.RemoveAt(i);
                            found = true;
                            resultMessage = $"❌ UID {uid} пользователя {username} отклонен!";
                            break;
                        }
                    }
                }

                if (found)
                {
                    File.WriteAllLines(ReferalsFilePath, lines);

                    await botClient.EditMessageText(
                        chatId: callbackQuery.Message.Chat.Id,
                        messageId: callbackQuery.Message.MessageId,
                        text: callbackQuery.Message.Text + $"\n\n{resultMessage}",
                        cancellationToken: cancellationToken
                    );

                    if (usernameToChatId.TryGetValue(username, out long userChatId))
                    {
                        string userMessage = action == "approve"
                            ? "🎉 Ваш UID одобрен! Теперь вы можете пользоваться всеми функциями бота!"
                            : "❌ Ваш UID был отклонен. Повторите попытку позже.";

                        await botClient.SendMessage(
                            chatId: userChatId,
                            text: userMessage,
                            cancellationToken: cancellationToken
                        );
                    }
                }

                await botClient.AnswerCallbackQuery(callbackQuery.Id, resultMessage, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в HandleCallbackQueryAsync: {ex.Message}");
                await botClient.AnswerCallbackQuery(callbackQuery.Id, "❌ Произошла ошибка", cancellationToken: cancellationToken);
            }
        }

        private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Ошибка: {exception.Message}");
            return Task.CompletedTask;
        }
    }
}