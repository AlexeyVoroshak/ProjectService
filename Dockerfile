# ============================================================
# Этап 1: Сборка (Build)
# ============================================================
# Используем SDK образ для компиляции проекта
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Копируем файл решения
COPY ["ProjectService.sln", ""]

# Копируем все csproj файлы и восстанавливаем зависимости
COPY ["ProjectService.API/ProjectService.API.csproj", "ProjectService.API/"]
COPY ["ProjectService.Application/ProjectService.Application.csproj", "ProjectService.Application/"]
COPY ["ProjectService.Infrastructure/ProjectService.Infrastructure.csproj", "ProjectService.Infrastructure/"]
COPY ["ProjectService.Domain/ProjectService.Domain.csproj", "ProjectService.Domain/"]

# Восстанавливаем NuGet пакеты
RUN dotnet restore "ProjectService.sln"

# Копируем весь исходный код
COPY . .

# Собираем приложение в Release режиме
WORKDIR "/src/ProjectService.API"
RUN dotnet build "ProjectService.API.csproj" -c Release -o /app/build

# ============================================================
# Этап 2: Публикация (Publish)
# ============================================================
# Публикуем приложение для продакшена
FROM build AS publish
RUN dotnet publish "ProjectService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ============================================================
# Этап 3: Финальный образ (Runtime)
# ============================================================
# Используем минимальный runtime образ
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Копируем опубликованные файлы
COPY --from=publish /app/publish .

# Открываем порт для HTTP
EXPOSE 8080
EXPOSE 8081

# Имя контейнера
ENV ASPNETCORE_URLS=http://+:8080

# точка входа
ENTRYPOINT ["dotnet", "ProjectService.API.dll"]
