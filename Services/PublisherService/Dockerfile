# ---------- Base runtime image ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Expose for documentation (Compose publishes 8082:8082)
EXPOSE 8082

# Kestrel must bind to all interfaces inside the container
# (Compose also sets ASPNETCORE_URLS, but this makes local runs predictable)
ENV ASPNETCORE_URLS=http://0.0.0.0:8082

# ---------- Build image ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore as separate layer (faster incremental builds)
COPY ["PublisherService.csproj", "./"]
RUN dotnet restore "PublisherService.csproj"

# Copy the rest of the source and publish
COPY . .
RUN dotnet publish "PublisherService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ---------- Final image ----------
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PublisherService.dll"]
