# Multi-stage build - Build stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["server/ClashSubManager.csproj", "./server/"]
RUN dotnet restore "./server/ClashSubManager.csproj"
COPY . .
WORKDIR "/src/server"
RUN dotnet build "ClashSubManager.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ClashSubManager.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM base AS final
WORKDIR /app

# Create non-root user
RUN addgroup --system --gid 1001 appgroup && \
    adduser --system --uid 1001 --ingroup appgroup appuser

# Create necessary directories
RUN mkdir -p /app/data /app/logs && \
    chown -R appuser:appgroup /app

# Copy published files
COPY --from=publish /app/publish .

# Set permissions
RUN chown -R appuser:appgroup /app

# Switch to non-root user
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:80/health || exit 1

# Start application
ENTRYPOINT ["dotnet", "ClashSubManager.dll"]
