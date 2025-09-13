FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -r linux-x64 -p:PublishAot=true --self-contained true -o /out

FROM debian:bookworm-slim
RUN useradd -m app && apt-get update && apt-get install -y ca-certificates && rm -rf /var/lib/apt/lists/*
USER app
WORKDIR /app
COPY --from=build /out .
ENTRYPOINT ["./SensorPublisher"]