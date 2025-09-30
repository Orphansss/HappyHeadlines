# syntax=docker/dockerfile:1

# ----- runtime base -----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8090

# ----- build stage -----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files first (restore cache)
COPY Services/DraftService/DraftService.csproj Services/DraftService/
COPY Infrastructure/Monitoring/Monitoring.csproj       Infrastructure/Monitoring/
RUN dotnet restore Services/DraftService/DraftService.csproj

# Copy the rest of the source
COPY . .
WORKDIR /src/Services/DraftService
RUN dotnet build DraftService.csproj -c $BUILD_CONFIGURATION -o /app/build --no-restore

# ----- publish stage -----
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR /src/Services/DraftService
RUN dotnet publish DraftService.csproj -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

# ----- final runtime image -----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DraftService.dll"]
