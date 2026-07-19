# =========================
# Стадия сборки
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем csproj отдельно, чтобы кэшировать restore
COPY CAT.csproj ./
RUN dotnet restore CAT.csproj

# Копируем остальной исходный код
COPY . ./

# Публикуем в Release
RUN dotnet publish CAT.csproj -c Release -o /app/publish

# =========================
# Стадия рантайма
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Копируем опубликованное приложение
COPY --from=build /app/publish .

# Приложение слушает на 5000 (nginx сюда будет стучаться)
ENV ASPNETCORE_URLS=http://0.0.0.0:5000
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 5000

ENTRYPOINT ["dotnet", "CAT.dll"]
