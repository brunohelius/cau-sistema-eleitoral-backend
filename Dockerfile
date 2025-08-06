# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY src/SistemaEleitoral.Api/*.csproj ./SistemaEleitoral.Api/
COPY src/SistemaEleitoral.Application/*.csproj ./SistemaEleitoral.Application/
COPY src/SistemaEleitoral.Domain/*.csproj ./SistemaEleitoral.Domain/
COPY src/SistemaEleitoral.Infrastructure/*.csproj ./SistemaEleitoral.Infrastructure/

WORKDIR /src/SistemaEleitoral.Api
RUN dotnet restore

# Copy everything else and build
WORKDIR /src
COPY src/ ./
WORKDIR /src/SistemaEleitoral.Api
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install required packages
RUN apt-get update && apt-get install -y \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Expose port
EXPOSE 8080

# Run the app
ENTRYPOINT ["dotnet", "SistemaEleitoral.Api.dll"]