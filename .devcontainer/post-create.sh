#!/bin/bash
set -e

echo "=== Fileway dev environment setup ==="

if [ -f "Fileway.sln" ]; then
    echo "Restoring NuGet packages..."
    dotnet restore
    echo "Building solution..."
    dotnet build --no-restore
else
    echo "No solution file yet — skipping restore"
fi

echo "Trusting HTTPS dev certificate..."
dotnet dev-certs https --trust 2>/dev/null || true

echo "Verifying LibreOffice..."
libreoffice --version

echo "Verifying Node.js..."
node --version

echo ""
echo "=== Fileway dev environment ready ==="
echo "  API:    http://localhost:5000"
echo "  Client: http://localhost:5001"
echo ""
