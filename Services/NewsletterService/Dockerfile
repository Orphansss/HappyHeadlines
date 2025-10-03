# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8083

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# restore layer (copy only csproj first)
COPY Infrastructure/Monitoring/Monitoring.csproj Infrastructure/Monitoring/
COPY Services/NewsletterService/NewsletterService.csproj Services/NewsletterService/
RUN dotnet restore Services/NewsletterService/NewsletterService.csproj

# copy the rest
COPY . .
WORKDIR /src/Services/NewsletterService
RUN dotnet build NewsletterService.csproj -c $BUILD_CONFIGURATION -o /app/build --no-restore

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish NewsletterService.csproj -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:8083
ENTRYPOINT ["dotnet", "NewsletterService.dll"]
