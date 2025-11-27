FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "./"]
COPY ["TradingApp.sln", "./"]
COPY ["src/BuildingBlocks/TradingApp.Contracts/TradingApp.Contracts.csproj", "src/BuildingBlocks/TradingApp.Contracts/"]
COPY ["src/BuildingBlocks/TradingApp.Kafka/TradingApp.Kafka.csproj", "src/BuildingBlocks/TradingApp.Kafka/"]
COPY ["src/BuildingBlocks/TradingApp.Elastic/TradingApp.Elastic.csproj", "src/BuildingBlocks/TradingApp.Elastic/"]
COPY ["src/Services/MarketAnalytics.Service/MarketAnalytics.Service.csproj", "src/Services/MarketAnalytics.Service/"]

RUN dotnet restore "src/Services/MarketAnalytics.Service/MarketAnalytics.Service.csproj"

COPY . .
RUN dotnet publish "src/Services/MarketAnalytics.Service/MarketAnalytics.Service.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app

ENV DOTNET_EnableDiagnostics=0 \
    DOTNET_ENVIRONMENT=Production \
    Kafka__BootstrapServers=kafka:9092 \
    Kafka__Consumer__Topic=raw-market-trades

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "TradingApp.MarketAnalytics.Service.dll"]


