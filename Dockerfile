# syntax=docker/dockerfile:1

# ----- Build stage -----------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only the project files first and restore. Docker caches this layer, so
# dependency restore is skipped on rebuilds unless a .csproj (or the shared
# build props) actually changes.
COPY Directory.Build.props ./
COPY src/Bookings.Domain/Bookings.Domain.csproj src/Bookings.Domain/
COPY src/Bookings.Application/Bookings.Application.csproj src/Bookings.Application/
COPY src/Bookings.Infrastructure/Bookings.Infrastructure.csproj src/Bookings.Infrastructure/
COPY src/Bookings.Api/Bookings.Api.csproj src/Bookings.Api/
RUN dotnet restore src/Bookings.Api/Bookings.Api.csproj

# Copy the remaining source and publish a framework-dependent build.
COPY src/ src/
RUN dotnet publish src/Bookings.Api/Bookings.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ----- Runtime stage ---------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# The aspnet:8.0 base image already runs as the non-root "app" user and listens
# on port 8080 by default.
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Bookings.Api.dll"]
