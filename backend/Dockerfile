FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/Tracewire.Domain/Tracewire.Domain.csproj src/Tracewire.Domain/
COPY src/Tracewire.Application/Tracewire.Application.csproj src/Tracewire.Application/
COPY src/Tracewire.Infrastructure/Tracewire.Infrastructure.csproj src/Tracewire.Infrastructure/
COPY src/Tracewire.Api/Tracewire.Api.csproj src/Tracewire.Api/
RUN dotnet restore src/Tracewire.Api/Tracewire.Api.csproj

COPY src/ src/
RUN dotnet publish src/Tracewire.Api/Tracewire.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Tracewire.Api.dll"]
