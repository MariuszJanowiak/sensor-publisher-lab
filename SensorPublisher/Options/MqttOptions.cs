namespace SensorPublisher.Options
{
    public sealed class MqttOptions
    {
        public string Host { get; set; } = "localhost";
        public string Topic { get; set; } = "lab/temperature";
        public int Port { get; set; } = 1883;
        public bool UseTls { get; set; } = false;
    }
}
