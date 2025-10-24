#!/bin/bash

# NhanViet Development Environment Setup Script
# This script sets up the development environment for NhanViet Labor Export Website

set -e

echo "🚀 Setting up NhanViet Development Environment..."

# Check if .NET 8.0 is installed
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET SDK is not installed. Please install .NET 8.0 SDK first."
    exit 1
fi

# Check .NET version
DOTNET_VERSION=$(dotnet --version)
echo "✅ .NET SDK version: $DOTNET_VERSION"

# Check if Docker is installed
if ! command -v docker &> /dev/null; then
    echo "❌ Docker is not installed. Please install Docker first."
    exit 1
fi

# Check if Docker Compose is installed
if ! command -v docker-compose &> /dev/null; then
    echo "❌ Docker Compose is not installed. Please install Docker Compose first."
    exit 1
fi

echo "✅ Docker and Docker Compose are installed"

# Create necessary directories
echo "📁 Creating necessary directories..."
mkdir -p App_Data/Sites/Default
mkdir -p Media
mkdir -p Localization
mkdir -p nginx

# Set permissions
chmod -R 755 App_Data
chmod -R 755 Media
chmod -R 755 Localization

echo "✅ Directories created and permissions set"

# Restore NuGet packages
echo "📦 Restoring NuGet packages..."
dotnet restore NhanVietSolution.sln

# Build the solution
echo "🔨 Building the solution..."
dotnet build NhanVietSolution.sln -c Debug

echo "✅ Solution built successfully"

# Create nginx configuration
echo "🌐 Creating nginx configuration..."
cat > nginx/nginx.conf << 'EOF'
events {
    worker_connections 1024;
}

http {
    upstream nhanviet_app {
        server nhanviet-website:80;
    }

    server {
        listen 80;
        server_name localhost;

        location / {
            proxy_pass http://nhanviet_app;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
        }
    }
}
EOF

echo "✅ Nginx configuration created"

# Create environment file
echo "⚙️ Creating environment configuration..."
cat > .env << 'EOF'
# NhanViet Environment Configuration
ASPNETCORE_ENVIRONMENT=Development
POSTGRES_DB=nhanviet_db
POSTGRES_USER=nhanviet_user
POSTGRES_PASSWORD=nhanviet_password
REDIS_CONNECTION=redis:6379
EOF

echo "✅ Environment configuration created"

# Start Docker services
echo "🐳 Starting Docker services..."
docker-compose up -d database redis

# Wait for database to be ready
echo "⏳ Waiting for database to be ready..."
sleep 10

# Run database migrations (if any)
echo "🗄️ Running database setup..."
# Note: OrchardCore will handle its own migrations on first run

echo "🎉 Development environment setup completed!"
echo ""
echo "📋 Next steps:"
echo "1. Run 'docker-compose up' to start all services"
echo "2. Or run 'dotnet run --project NhanViet.Website' for local development"
echo "3. Open http://localhost:8080 in your browser"
echo ""
echo "🔧 Useful commands:"
echo "- docker-compose up -d    : Start services in background"
echo "- docker-compose down     : Stop all services"
echo "- docker-compose logs     : View logs"
echo "- dotnet watch --project NhanViet.Website : Run with hot reload"