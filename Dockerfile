FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

COPY src/BotArbitragem.Domain/BotArbitragem.Domain.csproj src/BotArbitragem.Domain/
COPY src/BotArbitragem.Application/BotArbitragem.Application.csproj src/BotArbitragem.Application/
COPY src/BotArbitragem.Infrastructure/BotArbitragem.Infrastructure.csproj src/BotArbitragem.Infrastructure/
COPY src/BotArbitragem.Api/BotArbitragem.Api.csproj src/BotArbitragem.Api/
RUN dotnet restore src/BotArbitragem.Api/BotArbitragem.Api.csproj

COPY src/ src/
RUN dotnet publish src/BotArbitragem.Api/BotArbitragem.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0
EXPOSE 8080
COPY --from=build /app/publish .
USER app
ENTRYPOINT ["dotnet", "BotArbitragem.Api.dll"]
