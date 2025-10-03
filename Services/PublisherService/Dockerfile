# ---------- Base runtime image ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8082
ENV ASPNETCORE_URLS=http://0.0.0.0:8082

# ---------- Build image ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files for restore (must match repo paths, relative to build context = repo root)
COPY Services/PublisherService/PublisherService.csproj Services/PublisherService/
COPY Infrastructure/Monitoring/Monitoring.csproj       Infrastructure/Monitoring/

# Restore from the service csproj (which references Monitoring)
RUN dotnet restore Services/PublisherService/PublisherService.csproj

# Copy the rest of the source
COPY . .

# Build/Publish from the service folder
WORKDIR /src/Services/PublisherService
RUN dotnet publish PublisherService.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---------- Final image ----------
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PublisherService.dll"]
