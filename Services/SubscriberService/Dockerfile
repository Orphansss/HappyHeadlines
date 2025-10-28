# ---------- Base runtime image ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8091

# ---------- Build image ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files (for restore caching)
COPY Services/SubscriberService/SubscriberService.Api/SubscriberService.Api.csproj SubscriberService.Api/
COPY Services/SubscriberService/SubscriberService.Application/SubscriberService.Application.csproj SubscriberService.Application/
COPY Services/SubscriberService/SubscriberService.Domain/SubscriberService.Domain.csproj SubscriberService.Domain/
COPY Services/SubscriberService/SubscriberService.Infrastructure/SubscriberService.Infrastructure.csproj SubscriberService.Infrastructure/

# Restore dependencies
RUN dotnet restore SubscriberService.Api/SubscriberService.Api.csproj

# Copy the entire source tree
COPY Services/SubscriberService/. .

# Build the project
WORKDIR /src/SubscriberService.Api
RUN dotnet build SubscriberService.Api.csproj -c $BUILD_CONFIGURATION -o /app/build

# ---------- Publish stage ----------
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish SubscriberService.Api.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# ---------- Final runtime stage ----------
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:8091
ENTRYPOINT ["dotnet", "SubscriberService.Api.dll"]
