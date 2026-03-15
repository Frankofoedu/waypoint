FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/Waypoint.Domain/Waypoint.Domain.csproj src/Waypoint.Domain/
COPY src/Waypoint.Application/Waypoint.Application.csproj src/Waypoint.Application/
COPY src/Waypoint.Infrastructure/Waypoint.Infrastructure.csproj src/Waypoint.Infrastructure/
COPY src/Waypoint.Api/Waypoint.Api.csproj src/Waypoint.Api/
RUN dotnet restore src/Waypoint.Api/Waypoint.Api.csproj

COPY src/ src/
RUN dotnet publish src/Waypoint.Api/Waypoint.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Waypoint.Api.dll"]
