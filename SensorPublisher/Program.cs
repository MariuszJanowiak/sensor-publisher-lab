using SensorPublisher.Options;
using SensorPublisher.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddEnvironmentVariables();
builder.Services.Configure<MqttOptions>(builder.Configuration.GetSection("Mqtt"));
builder.Services.Configure<PublishOptions>(builder.Configuration.GetSection("Publish"));
builder.Services.Configure<SensorOptions>(builder.Configuration.GetSection("Sensor"));

builder.Services.AddHostedService<SensorService>();

var app = builder.Build();
await app.RunAsync();