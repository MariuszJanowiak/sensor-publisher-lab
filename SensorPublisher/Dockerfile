# SensorPublisher/Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /work
COPY ./SensorPublisher/*.csproj ./SensorPublisher/
RUN dotnet restore ./SensorPublisher/SensorPublisher.csproj

# Copy everything else and build
COPY . .
RUN dotnet publish ./SensorPublisher/SensorPublisher.csproj -c Release -o /out

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /out .
ENTRYPOINT ["dotnet","SensorPublisher.dll"]