# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# ---- restore layer: copy csproj with correct paths ----
COPY Services/ArticleService/ArticleService.csproj Services/ArticleService/
COPY Infrastructure/Monitoring/Monitoring.csproj       Infrastructure/Monitoring/
RUN dotnet restore Services/ArticleService/ArticleService.csproj

# ---- copy the rest ----
COPY . .
WORKDIR /src/Services/ArticleService
RUN dotnet build ArticleService.csproj -c $BUILD_CONFIGURATION -o /app/build --no-restore

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR /src/Services/ArticleService
RUN dotnet publish ArticleService.csproj -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet","ArticleService.dll"]
