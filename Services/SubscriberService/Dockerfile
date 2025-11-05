# ---------- Base runtime image ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8091

# ---------- Build image ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# (optional) copy solution for nicer restore output & caching
# COPY HappyHeadlines.sln ./

# Copy ONLY project files first to leverage layer caching on restore
COPY Services/SubscriberService/SubscriberService.Api/SubscriberService.Api.csproj Services/SubscriberService/SubscriberService.Api/
COPY Services/SubscriberService/SubscriberService.Application/SubscriberService.Application.csproj Services/SubscriberService/SubscriberService.Application/
COPY Services/SubscriberService/SubscriberService.Domain/SubscriberService.Domain.csproj Services/SubscriberService/SubscriberService.Domain/
COPY Services/SubscriberService/SubscriberService.Infrastructure/SubscriberService.Infrastructure.csproj Services/SubscriberService/SubscriberService.Infrastructure/
COPY Infrastructure/Monitoring/Monitoring.csproj Infrastructure/Monitoring/

# Restore the API (MSBuild will resolve the ProjectReference relatively)
RUN dotnet restore Services/SubscriberService/SubscriberService.Api/SubscriberService.Api.csproj

# Now copy the actual source code for those projects
COPY Services/SubscriberService/ Services/SubscriberService/
COPY Infrastructure/Monitoring/ Infrastructure/Monitoring/

# Build/Publish from the real path (preserve original structure)
WORKDIR /src/Services/SubscriberService/SubscriberService.Api
RUN dotnet publish -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# ---------- Final runtime stage ----------
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:8091
ENTRYPOINT ["dotnet", "SubscriberService.Api.dll"]
