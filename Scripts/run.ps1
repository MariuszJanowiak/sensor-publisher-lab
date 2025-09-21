param(
  [ValidateSet("host","infra")] [string]$Target = "host",
  [string]$Tag = "sensorpublisher:latest",
  [string]$Env = "Production",
  [string]$Topic = "lab/temperature",
  [int]$Port = 1883
)

if ($Target -eq "host") {
  docker run --rm --name sensor `
    -e DOTNET_ENVIRONMENT=$Env `
    -e Mqtt__Host=host.docker.internal `
    -e Mqtt__Port=$Port `
    -e Mqtt__Topic=$Topic `
    -e Mqtt__UseTls=false `
    $Tag
}
else {
  docker run --rm --name sensor --network infra_default `
    -e DOTNET_ENVIRONMENT=$Env `
    -e Mqtt__Host=mosquitto `
    -e Mqtt__Port=$Port `
    -e Mqtt__Topic=$Topic `
    -e Mqtt__UseTls=false `
    $Tag
}

<#

-Target host: myhost broker (App connect directly to host.docker.internal:1883).

-Target infra: broker at infra compose (App connec into mosquitto service within infra_default network).

-Env: set DOTNET_ENVIRONMENT (default Production).

-Topic, -Port: allows quickly switch within topics and ports - without pointless additional appsettings configuration.

#>