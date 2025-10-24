# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /source

# Copy solution and project files
COPY NhanVietSolution.sln .
COPY Directory.Build.props .
COPY NuGet.config .
COPY global.json .

# Copy all project files
COPY NhanViet.Website/*.csproj ./NhanViet.Website/
COPY NhanViet.JobOrders/*.csproj ./NhanViet.JobOrders/
COPY NhanViet.News/*.csproj ./NhanViet.News/
COPY NhanViet.Companies/*.csproj ./NhanViet.Companies/
COPY NhanViet.Recruitment/*.csproj ./NhanViet.Recruitment/
COPY NhanViet.Consultation/*.csproj ./NhanViet.Consultation/
COPY NhanViet.Countries/*.csproj ./NhanViet.Countries/
COPY NhanViet.Analytics/*.csproj ./NhanViet.Analytics/
COPY NhanViet.Frontend.Theme/*.csproj ./NhanViet.Frontend.Theme/
COPY NhanViet.Admin.Theme/*.csproj ./NhanViet.Admin.Theme/

# Restore packages
RUN dotnet restore NhanVietSolution.sln

# Copy source code
COPY . .

# Build and publish
RUN dotnet publish NhanViet.Website/NhanViet.Website.csproj -c Release -o /app --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install curl for health check
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create app user and SQLite directories
RUN adduser --disabled-password --gecos '' appuser && \
    mkdir -p /app/App_Data/Sites/Default && \
    chown -R appuser:appuser /app && \
    chmod -R 755 /app/App_Data

# Copy published app
COPY --from=build-env /app .

# Set proper ownership after copy
RUN chown -R appuser:appuser /app && \
    chmod -R 755 /app/App_Data

USER appuser

# Expose port
EXPOSE 80
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost/ || exit 1

ENTRYPOINT ["dotnet", "NhanViet.Website.dll"]