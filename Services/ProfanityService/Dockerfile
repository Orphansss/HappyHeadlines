# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# --- copy csproj FIRST (for fast layer caching) ---
COPY Services/ProfanityService/ProfanityService.csproj Services/ProfanityService/
COPY Infrastructure/Monitoring/Monitoring.csproj       Infrastructure/Monitoring/

# --- restore only the service (will pull Monitoring via ProjectReference) ---
RUN dotnet restore Services/ProfanityService/ProfanityService.csproj

# --- now copy the rest of the source ---
COPY . .
WORKDIR /src/Services/ProfanityService

RUN dotnet build ProfanityService.csproj -c $BUILD_CONFIGURATION -o /app/build --no-restore

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR /src/Services/ProfanityService
RUN dotnet publish ProfanityService.csproj -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProfanityService.dll"]
