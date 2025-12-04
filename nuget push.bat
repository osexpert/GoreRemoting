REM set myKey=
set ver=0.0.5

nuget push bin\Packages\Release\NuGet\GoreRemoting.%ver%.nupkg -src https://api.nuget.org/v3/index.json -ApiKey %myKey%
nuget push bin\Packages\Release\NuGet\GoreRemoting.Compression.Lz4.%ver%.nupkg -src https://api.nuget.org/v3/index.json -ApiKey %myKey%
nuget push bin\Packages\Release\NuGet\GoreRemoting.Serialization.BinaryFormatter.%ver%.nupkg -src https://api.nuget.org/v3/index.json -ApiKey %myKey%
nuget push bin\Packages\Release\NuGet\GoreRemoting.Serialization.Json.%ver%.nupkg -src https://api.nuget.org/v3/index.json -ApiKey %myKey%
nuget push bin\Packages\Release\NuGet\GoreRemoting.Serialization.MemoryPack.%ver%.nupkg -src https://api.nuget.org/v3/index.json -ApiKey %myKey%
nuget push bin\Packages\Release\NuGet\GoreRemoting.Serialization.MessagePack.%ver%.nupkg -src https://api.nuget.org/v3/index.json -ApiKey %myKey%
nuget push bin\Packages\Release\NuGet\GoreRemoting.Serialization.Protobuf.%ver%.nupkg -src https://api.nuget.org/v3/index.json -ApiKey %myKey%