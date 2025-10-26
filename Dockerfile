# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["Loopai.CloudApi.sln", "./"]
COPY ["src/Loopai.Core/Loopai.Core.csproj", "src/Loopai.Core/"]
COPY ["src/Loopai.CloudApi/Loopai.CloudApi.csproj", "src/Loopai.CloudApi/"]
COPY ["tests/Loopai.CloudApi.Tests/Loopai.CloudApi.Tests.csproj", "tests/Loopai.CloudApi.Tests/"]

# Restore dependencies
RUN dotnet restore "Loopai.CloudApi.sln"

# Copy source code
COPY . .

# Build and publish
WORKDIR "/src/src/Loopai.CloudApi"
RUN dotnet publish "Loopai.CloudApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install Deno for Edge Runtime execution
RUN apt-get update && \
    apt-get install -y curl unzip && \
    curl -fsSL https://deno.land/install.sh | sh && \
    mv /root/.deno/bin/deno /usr/local/bin/ && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN groupadd -r loopai && useradd -r -g loopai loopai

# Copy published application
COPY --from=build /app/publish .

# Change ownership to non-root user
RUN chown -R loopai:loopai /app

# Switch to non-root user
USER loopai

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Environment variables
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "Loopai.CloudApi.dll"]
