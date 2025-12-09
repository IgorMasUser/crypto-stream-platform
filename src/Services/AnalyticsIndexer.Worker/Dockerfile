FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "./"]
COPY ["TradingApp.sln", "./"]
COPY ["src/BuildingBlocks/TradingApp.Contracts/TradingApp.Contracts.csproj", "src/BuildingBlocks/TradingApp.Contracts/"]
COPY ["src/BuildingBlocks/TradingApp.Kafka/TradingApp.Kafka.csproj", "src/BuildingBlocks/TradingApp.Kafka/"]
COPY ["src/BuildingBlocks/TradingApp.Elastic/TradingApp.Elastic.csproj", "src/BuildingBlocks/TradingApp.Elastic/"]
COPY ["src/Services/AnalyticsIndexer.Worker/AnalyticsIndexer.Worker.csproj", "src/Services/AnalyticsIndexer.Worker/"]

RUN dotnet restore "src/Services/AnalyticsIndexer.Worker/AnalyticsIndexer.Worker.csproj"

COPY . .
RUN dotnet publish "src/Services/AnalyticsIndexer.Worker/AnalyticsIndexer.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TradingApp.AnalyticsIndexer.Worker.dll"]

