FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "./"]
COPY ["TradingApp.sln", "./"]
COPY ["src/BuildingBlocks/TradingApp.Contracts/TradingApp.Contracts.csproj", "src/BuildingBlocks/TradingApp.Contracts/"]
COPY ["src/BuildingBlocks/TradingApp.Kafka/TradingApp.Kafka.csproj", "src/BuildingBlocks/TradingApp.Kafka/"]
COPY ["src/BuildingBlocks/TradingApp.Elastic/TradingApp.Elastic.csproj", "src/BuildingBlocks/TradingApp.Elastic/"]
COPY ["src/Services/MarketData.Ingestor/MarketData.Ingestor.csproj", "src/Services/MarketData.Ingestor/"]
COPY ["src/Services/MarketAnalytics.Service/MarketAnalytics.Service.csproj", "src/Services/MarketAnalytics.Service/"]
COPY ["src/Services/AnalyticsIndexer.Worker/AnalyticsIndexer.Worker.csproj", "src/Services/AnalyticsIndexer.Worker/"]
COPY ["src/Services/Dashboard.Web/Dashboard.Web.csproj", "src/Services/Dashboard.Web/"]

RUN dotnet restore "src/Services/Dashboard.Web/Dashboard.Web.csproj"

COPY . .
RUN dotnet publish "src/Services/Dashboard.Web/Dashboard.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TradingApp.Dashboard.Web.dll"]

