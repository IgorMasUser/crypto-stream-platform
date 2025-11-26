FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# copy solution + props first to leverage layer caching
COPY ["Directory.Build.props", "./"]
COPY ["TradingApp.sln", "./"]

# copy csproj files to warm restore cache
COPY ["src/BuildingBlocks/TradingApp.Contracts/TradingApp.Contracts.csproj", "src/BuildingBlocks/TradingApp.Contracts/"]
COPY ["src/BuildingBlocks/TradingApp.Kafka/TradingApp.Kafka.csproj", "src/BuildingBlocks/TradingApp.Kafka/"]
COPY ["src/BuildingBlocks/TradingApp.Elastic/TradingApp.Elastic.csproj", "src/BuildingBlocks/TradingApp.Elastic/"]
COPY ["src/Services/MarketData.Ingestor/MarketData.Ingestor.csproj", "src/Services/MarketData.Ingestor/"]

RUN dotnet restore "src/Services/MarketData.Ingestor/MarketData.Ingestor.csproj"

# copy the remaining sources and publish
COPY . .
RUN dotnet publish "src/Services/MarketData.Ingestor/MarketData.Ingestor.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app

# default env vars (can be overridden at runtime)
ENV DOTNET_EnableDiagnostics=0 \
    DOTNET_ENVIRONMENT=Production \
    Kafka__BootstrapServers=kafka:9092 \
    Binance__Symbols=btcusdt,ethusdt,bnbusdt,xrpusdt,solusdt

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "TradingApp.MarketData.Ingestor.dll"]

