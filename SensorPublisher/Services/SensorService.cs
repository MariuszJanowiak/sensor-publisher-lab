using MQTTnet;
using MQTTnet.Client;
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

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            #region Builder

            var factory = new MqttFactory();
            _client = factory.CreateMqttClient();

            var builder = new MqttClientOptionsBuilder()
                .WithTcpServer(_mqtt.Host, _mqtt.Port)
                .WithClientId($"sensor-{Guid.NewGuid():N}")
                .WithTlsOptions(o =>
                {
                     o.UseTls(_mqtt.UseTls);
                });

            var options = builder.Build();

            _client.DisconnectedAsync += async e => 
            {
                log.LogWarning(e.Exception + "MQTT disconnected. Reconnecting");
                var delay = TimeSpan.FromSeconds(3);

                //TODO:: Reconnecting part
            };

            #endregion

            #region CancellationToken Loops

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await _client.ConnectAsync(options, ct);
                    log.LogInformation("MQTT connected: {Host}:{Port} TLS={Tls}", _mqtt.Host, _mqtt.Port, _mqtt.UseTls);
                    break;
                }
                catch (Exception ex)
                {
                    log.LogWarning(ex, "MQTT not ready, retry in 3s…");
                    await Task.Delay(3000, ct);
                }
            }

            while (!ct.IsCancellationRequested)
            {
                var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var t = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var value = 22.0 + 3.0 * Math.Sin(t / 30.0) + _random.NextDouble();

                var payload = System.Text.Json.JsonSerializer.Serialize(new
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

                try
                {
                    await _client!.PublishAsync(msg, ct);
                    log.LogInformation("→ {Topic} {Val:F2}°C", _mqtt.Topic, value);
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Publish failed");
                }

                await Task.Delay(TimeSpan.FromSeconds(_pub.PeriodSeconds), ct);
            }

            #endregion
        }

        public override async Task StopAsync(CancellationToken ct)
        {
            if (_client?.IsConnected == true)
            {
                var disc = new MqttClientDisconnectOptions
                {
                    Reason = MqttClientDisconnectOptionsReason.NormalDisconnection
                };
                await _client.DisconnectAsync(disc, ct);
            }

            await base.StopAsync(ct);
        }
    }
}
