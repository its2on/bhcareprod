# Use the official .NET 8.0 runtime as the base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the official .NET 8.0 SDK as the build image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies
COPY ["Barangay.csproj", "."]
RUN dotnet restore "Barangay.csproj"

# Copy the rest of the source code
COPY . .

# Build the application
RUN dotnet build "Barangay.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "Barangay.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Create the final runtime image
FROM base AS final
WORKDIR /app

# Switch to root to install packages
USER root

# Install Tesseract OCR, Leptonica, and OpenCV native libraries
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        tesseract-ocr \
        tesseract-ocr-eng \
        libleptonica-dev \
        libtesseract-dev \
        libopencv-dev \
        libopencv4 \
        libgdiplus \
        libc6-dev && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

# Create a non-root user for security
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "Barangay.dll"]
