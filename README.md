# SensorPublisherLab
Hello there!  
Welcome in my **tiny** laboratory that been constructed for practice and schoolar reasons being a minimal IoT pipeline with .NET 8 that:  
![lab_badge](https://img.shields.io/badge/lab%20-%20.net%20MQQT%20Iot%20-%20green%20?style=flat-square&logoColor=green&label=lab&labelColor=white)
- simulates **sensor** data,
- publishes to **MQTT** (Mosquitto broker),
- stores metrics in **InfluxDB**,
- visualizes them in **Grafana**,
- runs locally and in **Docker Compose**.

# Architecture
```
sensor-publisher-lab/
│
├─ SensorPublisher/ # .NET 8 WorkerService application
│ ├─ Options/ # POCO option classes mapped from config
│ ├─ Services/ # SensorService (publishes to MQTT)
│ ├─ appsettings*.json # Configurations (Development, Production, Stable)
│ ├─ Program.cs # Service and options registration
│ └─ Dockerfile # Application Dockerfile
│
├─ Infra/ # Docker infrastructure
│ ├─ docker-compose.yml # Mosquitto + InfluxDB + Telegraf + Grafana
│ ├─ mosquitto/ # Broker configuration
│ └─ telegraf/ # Telegraf configuration
│
├─ scripts/ # PowerShell helper scripts
│ ├─ build.ps1 # Build the Docker image
│ └─ run.ps1 # Run the app (host/infra modes)
│
├─ README.md # Documentation (this file)
└─ SensorPublisher.sln # Visual Studio 2022 solution
```
# How it works?
- SensorPublisher simulates temperature data (sinusoidal + noise).
- Data is published as JSON messages to MQTT (Mosquitto broker).
- Telegraf subscribes to the topic and writes data to InfluxDB.
- Grafana queries InfluxDB (Flux) to visualize metrics.

# Tech stack
.NET 8 WorkerService — BackgroundService, DI, Options Pattern  
MQTTnet — MQTT client in C#  
Mosquitto — MQTT broker  
Telegraf — agent: input MQTT → output InfluxDB  
InfluxDB 2.x — time series database  
Grafana — dashboard  
Docker — orchestration for the stack  

## Build
pwsh -ExecutionPolicy Bypass -File .\scripts\build.ps1

## Run
### Using host broker:
pwsh -ExecutionPolicy Bypass -File .\scripts\run.ps1 -Target host

### Using infra network:
pwsh -ExecutionPolicy Bypass -File .\scripts\run.ps1 -Target infra

## Grafana Dashboard:
Data source URL:  
http://influxdb:8086  

Query: 
```
from(bucket: "sensors")
  |> range(start: -15m)
  |> filter(fn: (r) => r._measurement == "temperature" and r._field == "value")
  |> aggregateWindow(every: 10s, fn: mean)
  |> yield(name: "mean")
```

## Configuration
appsetting.*.Json  
```
"Mqtt": { "Host": "mosquitto", "Port": 1883, "Topic": "lab/temperature", "UseTls": false },
"Publish": { "PeriodSeconds": 2 },
"Sensor": { "Name": "sim1", "Site": "labA" }
```
Just keep in mind that - it's just lab :)  
Architecture and way how it's implemented ain't perfect - lack of proper separation, DI, Interface, Port, Transport layers - that's more than obvious :)
