param(
  [string]$Tag = "sensorpublisher:latest",
  [string]$Dockerfile = "SensorPublisher/Dockerfile"
)
docker build -t $Tag -f $Dockerfile .

<# 

Flags:
-t Subscribe tag builded image (name:version)
-f Pointing Dockerfile at subfolder
. It's context(entire repository)

#>