using MQTTnet;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SensorPublisher.Options;

namespace SensorPublisher.Services
{
    public sealed class SensorService(
        IOptions<MqttOptions> mqttOptions,
        IOptions<PublishOptions> pubOptions,
        IOptions<SensorOptions> sensorOptions,
        ILogger<SensorService> log
    ) : BackgroundService
    {
        private readonly MqttOptions _mqtt = mqttOptions.Value;
        private readonly PublishOptions _pub = pubOptions.Value;
        private readonly SensorOptions _sensor = sensorOptions.Value;

        private IMqttClient? _client;
        private readonly Random _random = new();

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _client = new MqttClientFactory().CreateMqttClient();

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(_mqtt.Host, _mqtt.Port)
                .WithClientId($"sensor-{Guid.NewGuid():N}")
                .WithTlsOptions(option => {option.UseTls(_mqtt.UseTls);})
                .Build();

            _client.ConnectedAsync += e =>
            {
                log.LogInformation("MQTT connected. SessionPresent={Session}",
                    e.ConnectResult?.IsSessionPresent);
                return Task.CompletedTask;
            };

            _client.DisconnectedAsync += async e =>
            {
                log.LogWarning(e.Exception, "MQTT disconnected. Reconnecting…");

                int delaySec = 3;
                const int maxSec = 30;

                while (!_client!.IsConnected && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(delaySec), cancellationToken);
                        await _client.ConnectAsync(options, cancellationToken);
                        log.LogInformation("MQTT reconnected.");
                        break;
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        log.LogWarning(ex, "Reconnection failed; retry in {Delay}s", delaySec);
                        delaySec = Math.Min(delaySec * 2, maxSec); // 3→6→12→24→30
                    }
                }
            };

            while (!cancellationToken.IsCancellationRequested) // Connect loop
            {
                try
                {
                    await _client.ConnectAsync(options, cancellationToken);
                    log.LogInformation("MQTT connected: {Host}:{Port} TLS={Tls}", _mqtt.Host, _mqtt.Port, _mqtt.UseTls);
                    break;
                }
                catch (Exception ex)
                {
                    log.LogWarning(ex, "MQTT not ready, retry in 3s…");
                    await Task.Delay(3000, cancellationToken);
                }
            }

            while (!cancellationToken.IsCancellationRequested) // Publish loop
            {
                try
                {
                    if (!_client!.IsConnected) // Check connection
                    {
                        await Task.Delay(200, cancellationToken);
                        continue;
                    }

                    var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    var timeS = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    var value = 22.0 + 3.0 * Math.Sin(timeS / 30.0) + _random.NextDouble();

                    var payload = JsonSerializer.Serialize(new
                    {
                        measurement = "temperature",
                        value,
                        sensor = _sensor.Name,
                        unit = "C",
                        site = _sensor.Site,
                        ts = nowMs
                    });

                    var msg = new MqttApplicationMessageBuilder()
                        .WithTopic(_mqtt.Topic)
                        .WithPayload(System.Text.Encoding.UTF8.GetBytes(payload))
                        .Build();

                    await _client!.PublishAsync(msg, cancellationToken);
                    log.LogInformation("→ {Topic} {Val:F2}°C", _mqtt.Topic, value);
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Publish failed");
                }

                await Task.Delay(TimeSpan.FromSeconds(_pub.PeriodSeconds), cancellationToken);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationTokent)
        {
            if (_client?.IsConnected == true)
            {
                var disconnectOptions = new MqttClientDisconnectOptions
                {
                    Reason = MqttClientDisconnectOptionsReason.NormalDisconnection
                };
                await _client.DisconnectAsync(disconnectOptions, cancellationTokent);
            }

            await base.StopAsync(cancellationTokent);
        }
    }
}
