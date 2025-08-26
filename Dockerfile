# Этап сборки
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем решение и проект
COPY TelegramBot.sln ./
COPY TelegramBot/ TelegramBot/

# Восстанавливаем зависимости
RUN dotnet restore TelegramBot/TelegramBot.csproj

# Сборка и публикация
RUN dotnet publish TelegramBot/TelegramBot.csproj -c Release -o /app/publish

# Этап запуска
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app

RUN apt-get update && apt-get install -y \
    libfontconfig1 \
    libfreetype6 \
    libharfbuzz0b \
    libpng16-16 \
    libjpeg62-turbo \
    libgif7 \
    libwebp7 \
    libtiff6 \
    libxcb1 \
    libx11-6 \
    libxext6 \
    libxrender1 \
    libxrandr2 \
    libxi6 \
    && rm -rf /var/lib/apt/lists/*

# Копируем собранный проект
COPY --from=build /app/publish .

# Копируем статические файлы (фото и txt)
COPY TelegramBot/bin/Debug/net8.0/Source ./Source

# Запуск бота
ENTRYPOINT ["dotnet", "TelegramBot.dll"]